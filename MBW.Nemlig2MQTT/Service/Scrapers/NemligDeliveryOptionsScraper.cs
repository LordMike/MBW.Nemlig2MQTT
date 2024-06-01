using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligDeliveryOptionsScraper : IResponseScraper
{
    record struct DateOption(float Score, Dayhour DeliveryTime);

    private readonly NemligConfiguration _config;
    private readonly ILogger<NemligDeliveryOptionsScraper> _logger;
    private readonly NemligClient _nemligClient;
    private readonly HassMqttManager _hassMqttManager;
    private readonly DeliveryRenderer _deliveryRenderer;
    private readonly Dictionary<string, int> _callbackLookup = new(StringComparer.Ordinal);

    private readonly IDiscoveryDocumentBuilder<MqttSelect> _deliverySelectConfig;
    private readonly ISensorContainer _deliverySelect;
    private readonly BitArray _prioritizeHours;

    public NemligDeliveryOptionsScraper(ILogger<NemligDeliveryOptionsScraper> logger, IOptions<NemligConfiguration> config, NemligClient nemligClient, HassMqttManager hassMqttManager, DeliveryRenderer deliveryRenderer)
    {
        _config = config.Value;
        _logger = logger;
        _nemligClient = nemligClient;
        _hassMqttManager = hassMqttManager;
        _deliveryRenderer = deliveryRenderer;

        _prioritizeHours = new BitArray(24);
        if (_config.DeliveryConfig.PrioritizeHours != null)
        {
            foreach (byte hour in _config.DeliveryConfig.PrioritizeHours)
                _prioritizeHours[hour] = true;
        }

        _deliverySelectConfig = _hassMqttManager.ConfigureSensor<MqttSelect>(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery_select")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes, HassTopicKind.Command)
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Nemlig delivery time picker";
                discovery.Options = Array.Empty<string>();
            })
            .ConfigureAliveService();

        _deliverySelect = _deliverySelectConfig.GetSensor();
    }

    public async Task SetValue(string chosenValue, CancellationToken token = default)
    {
        if (!_callbackLookup.TryGetValue(chosenValue, out int id))
        {
            _logger.LogWarning("Unable to identify callback value {Value}", chosenValue);
            return;
        }

        _logger.LogDebug("Updating delivery time to {Id}, timestamp {DeliveryTime}", id, DateTime.UtcNow);

        await _nemligClient.TryUpdateDeliveryTime(id, token);
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not NemligDeliveryDaysResponse deliveryDetails)
            return Task.CompletedTask;

        DateTime priorityBounds = DateTime.UtcNow.AddHours(_config.DeliveryConfig.PrioritizeMaxHours);

        // Lowest 10% / 5 values is low
        float lowPrice;
        {
            List<float> allPrices = deliveryDetails.DayRangeHours
                .SelectMany(s => s.DayHours)
                .Select(s => s.DeliveryPrice)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            lowPrice = allPrices.Take(Math.Min(5, allPrices.Count / 10)).Max();
        }

        bool isLow(float price) => Math.Abs(lowPrice - price) < float.Epsilon;

        // Score all options
        List<DateOption> allOptions = deliveryDetails.DayRangeHours
            .SelectMany(s => s.DayHours)
            .Where(s => s.Availability == NemligDeliveryAvailability.Available)
            .Where(s => _config.DeliveryConfig.AllowDeliveryTypes.Contains(s.Type))
            .Where(s => !_config.DeliveryConfig.MaxDeliveryPrice.HasValue || s.DeliveryPrice <= _config.DeliveryConfig.MaxDeliveryPrice)
            .Select(s =>
            {
                float score = 0f;
                if (s.Date <= priorityBounds)
                    score += 0.1f;

                // Priorize sooner, such that 0hours = 0.1f, 4days+ = 0.0f
                score += 0.1f - (float)Math.Clamp((s.Date - DateTime.UtcNow).TotalHours / 96f / 10, 0, 0.1f);

                if (_config.DeliveryConfig.PrioritizeShortTimespan && s.NumberOfHours <= 3)
                    score += 0.2f;

                if (_config.DeliveryConfig.PrioritizeFreeDelivery && Math.Abs(s.DeliveryPrice) < float.Epsilon)
                    score += 0.1f;

                if (_prioritizeHours[s.StartHour] && _prioritizeHours[s.EndHour])
                    score += 0.2f;

                if (_config.DeliveryConfig.PrioritizeCheapHours && isLow(s.DeliveryPrice))
                    score += 0.3f;

                return new DateOption(score, s);
            })
            .OrderByDescending(s => s.Score)
            .ToList();

        // Track all options
        _callbackLookup.Clear();
        allOptions.ForEach(s => _callbackLookup.Add(_deliveryRenderer.Render(s.DeliveryTime), s.DeliveryTime.Id));

        // Identify best options
        DateOption? currentSelected = allOptions.Cast<DateOption?>().FirstOrDefault(s => s.Value.DeliveryTime.IsSelected);
        List<Dayhour> bestOptions = Enumerable.Empty<DateOption>()
            // Take the best
            .Append(allOptions.First())
            // Take the currently picked
            .Append(currentSelected ?? default)
            // Take the best, for each day
            .Concat(allOptions.GroupBy(s => s.DeliveryTime.Date.Date).Select(s => s.OrderByDescending(x => x.Score).First()))
            // Take the best, for each days night hours (this is prob. only unattended, but should be included always)
            .Concat(allOptions.Where(s => s.DeliveryTime.StartHour <= 7).GroupBy(s => s.DeliveryTime.Date.Date).Select(s => s.OrderByDescending(x => x.Score).First()))
            .Where(s => s.DeliveryTime != null)
            .Select(s => s.DeliveryTime)
            .Distinct()
            .OrderBy(s => s.Date)
            .ToList();

        string[] lst = bestOptions.Select(_deliveryRenderer.Render).ToArray();

        _deliverySelectConfig.Discovery.Options = lst;

        _logger.LogDebug("Updating delivery options to {DeliveryOptions}", new object[]{ lst });

        if (currentSelected.HasValue)
            _deliverySelect.SetValue(HassTopicKind.State, _deliveryRenderer.Render(currentSelected.Value.DeliveryTime));
        else
            _deliverySelect.SetValue(HassTopicKind.State, lst.First());

        return Task.CompletedTask;
    }
}