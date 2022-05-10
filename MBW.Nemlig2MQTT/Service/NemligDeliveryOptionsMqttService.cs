using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Checkout;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace MBW.Nemlig2MQTT.Service
{
    internal class NemligDeliveryOptionsMqttService : BackgroundService
    {
        [DebuggerDisplay("{Score}: {DeliveryTime.Date:yyyy-MM-dd-HH}, {DeliveryTime.DeliveryPrice}DKK")]
        record struct DateOption(float Score, Dayhour DeliveryTime);

        private readonly ILogger<NemligDeliveryOptionsMqttService> _logger;
        private readonly NemligClient _nemligClient;
        private readonly HassMqttManager _hassMqttManager;
        private readonly DeliveryRenderer _deliveryRenderer;
        private readonly ApiOperationalContainer _apiOperationalContainer;
        private readonly AsyncAutoResetEvent _syncEvent = new AsyncAutoResetEvent();
        private readonly NemligDeliveryConfiguration _config;
        private readonly HassConfiguration _hassConfig;
        private readonly BitArray _prioritizeHours;
        private readonly Dictionary<string, int> _callbackLookup = new Dictionary<string, int>(StringComparer.Ordinal);

        private IDiscoveryDocumentBuilder<MqttSelect> _deliverySelectConfig;
        private ISensorContainer _deliverySelect;

        public NemligDeliveryOptionsMqttService(
            ILogger<NemligDeliveryOptionsMqttService> logger,
            IOptions<NemligDeliveryConfiguration> config,
            IOptions<HassConfiguration> hassConfig,
            NemligClient nemligClient,
            HassMqttManager hassMqttManager,
            ApiOperationalContainer apiOperationalContainer,
            DeliveryRenderer deliveryRenderer)
        {
            _logger = logger;
            _nemligClient = nemligClient;
            _hassMqttManager = hassMqttManager;
            _apiOperationalContainer = apiOperationalContainer;
            _deliveryRenderer = deliveryRenderer;
            _config = config.Value;
            _hassConfig = hassConfig.Value;

            _prioritizeHours = new BitArray(24);
            if (_config.PrioritizeHours != null)
            {
                foreach (byte hour in _config.PrioritizeHours)
                    _prioritizeHours[hour] = true;
            }
        }

        public void ForceSync()
        {
            _syncEvent.Set();
        }

        public async Task SetValue(string chosenValue, CancellationToken token = default)
        {
            _callbackLookup.TryGetValue(chosenValue, out var id);

            await _nemligClient.TryUpdateDeliveryTime(id, token);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateEntities();

            {
                _logger.LogDebug("Updating stored cards once");

                NemligCreditCard[] cardsResponse = await _nemligClient.GetCreditCards(token: stoppingToken);

                CreateEntities(cardsResponse);
            }

            // Update loop
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Beginning update");

                try
                {
                    NemligDeliveryDaysResponse deliveryDetails = await _nemligClient.GetDeliveryDays(_config.DaysToCheck, stoppingToken);

                    Update(deliveryDetails);

                    // Track API operational status
                    _apiOperationalContainer.MarkOk();

                    await _hassMqttManager.FlushAll(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while performing the update");

                    // Track API operational status
                    _apiOperationalContainer.MarkError(e.Message);
                }

                try
                {
                    using CancellationTokenSource cts = new CancellationTokenSource(_config.CheckInterval);
                    using CancellationTokenSource combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

                    await _syncEvent.WaitAsync(combinedCancel.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Update(NemligDeliveryDaysResponse deliveryDetails)
        {
            DateTime priorityBounds = DateTime.UtcNow.AddHours(_config.PrioritizeMaxHours);

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
                .Where(s => _config.AllowDeliveryTypes.Contains(s.Type))
                .Where(s => !_config.MaxDeliveryPrice.HasValue || s.DeliveryPrice <= _config.MaxDeliveryPrice)
                .Select(s =>
                {
                    float score = 0f;
                    if (s.Date <= priorityBounds)
                        score += 0.1f;

                    // Priorize sooner, such that 0hours = 0.1f, 4days+ = 0.0f
                    score += 0.1f - (float)Math.Clamp((s.Date - DateTime.UtcNow).TotalHours / 96f / 10, 0, 0.1f);

                    if (_config.PrioritizeShortTimespan && s.NumberOfHours <= 3)
                        score += 0.2f;

                    if (_config.PrioritizeFreeDelivery && Math.Abs(s.DeliveryPrice) < float.Epsilon)
                        score += 0.1f;

                    if (_prioritizeHours[s.StartHour] && _prioritizeHours[s.EndHour])
                        score += 0.2f;

                    if (_config.PrioritizeCheapHours && isLow(s.DeliveryPrice))
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

            if (currentSelected.HasValue)
                _deliverySelect.SetValue(HassTopicKind.State, _deliveryRenderer.Render(currentSelected.Value.DeliveryTime));
            else
                _deliverySelect.SetValue(HassTopicKind.State, lst.First());
        }

        private void CreateEntities()
        {
            _deliverySelectConfig = _hassMqttManager.ConfigureSensor<MqttSelect>(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery_select")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes, HassTopicKind.Command)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Nemlig delivery time picker";
                    discovery.Options = Array.Empty<string>();
                })
                .ConfigureAliveService();

            _deliverySelect = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery_select");
        }

        private void CreateEntities(NemligCreditCard[] cardsResponse)
        {
            // Create one button / service for each card
            foreach (NemligCreditCard card in cardsResponse)
            {
                _hassMqttManager.ConfigureSensor<MqttButton>(HassUniqueIdBuilder.GetBasketDeviceId(), $"complete_order_cc_{card.CardId}")
                    .ConfigureTopics(HassTopicKind.JsonAttributes)
                    .ConfigureBasketDevice()
                    .ConfigureDiscovery(discovery =>
                    {
                        discovery.Name = $"Nemlig order with card {card.CardMask}";
                        discovery.CommandTopic = $"{_hassConfig.TopicPrefix}/basket/order-cc/{card.CardId}";
                        //discovery.CommandTemplate = "{}";
                    })
                    .ConfigureAliveService();

                ISensorContainer sensor = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), $"complete_order_cc_{card.CardId}");

                sensor.SetAttribute("card_expires", card.CardExpirationInfo);
                sensor.SetAttribute("card_expire_year", card.CardExpirationYear);
                sensor.SetAttribute("card_expire_month", card.CardExpirationMonth);
                sensor.SetAttribute("card_type", card.CardType);
                sensor.SetAttribute("card_isdefault", card.IsDefault);
                sensor.SetAttribute("fee_percent", card.FeeInPercent);
            }
        }
    }
}