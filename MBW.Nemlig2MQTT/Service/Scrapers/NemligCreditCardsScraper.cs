using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Checkout;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligCreditCardsScraper : IResponseScraper
{
    private readonly HassConfiguration _config;
    private readonly ILogger<NemligCreditCardsScraper> _logger;
    private readonly HassMqttManager _hassMqttManager;
    private readonly HashSet<int> _seen = new();

    public NemligCreditCardsScraper(ILogger<NemligCreditCardsScraper> logger, IOptions<HassConfiguration> config, HassMqttManager hassMqttManager)
    {
        _config = config.Value;
        _logger = logger;
        _hassMqttManager = hassMqttManager;
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not NemligCreditCard[] cardsResponse)
            return Task.CompletedTask;

        // Create one button / service for each card
        foreach (NemligCreditCard card in cardsResponse)
        {
            if (!_seen.Add(card.CardId))
                continue;

            _logger.LogInformation("Creating button to order with credit card {CardId}, mask: {CardMask}. Default: {IsDefault}", card.CardId, card.CardMask, card.IsDefault);

            _hassMqttManager.ConfigureSensor<MqttButton>(HassUniqueIdBuilder.GetBasketDeviceId(), $"complete_order_cc_{card.CardId}")
                .ConfigureTopics(HassTopicKind.JsonAttributes)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"Nemlig order with card {card.CardMask}";
                    discovery.CommandTopic = $"{_config.TopicPrefix}/basket/order-cc/{card.CardId}";
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

        return Task.CompletedTask;
    }
}