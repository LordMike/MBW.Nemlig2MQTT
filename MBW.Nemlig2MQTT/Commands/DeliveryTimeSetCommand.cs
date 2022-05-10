using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands;

internal class DeliveryTimeSetCommand : IMqttCommandHandler
{
    private readonly ILogger<DeliveryTimeSetCommand> _logger;
    private readonly NemligDeliveryOptionsScraper _scraper;
    private readonly NemligClient _nemligClient;
    private readonly ScraperManager _scrapers;

    public DeliveryTimeSetCommand(ILogger<DeliveryTimeSetCommand> logger, NemligDeliveryOptionsScraper scraper, NemligClient nemligClient, ScraperManager scrapers)
    {
        _logger = logger;
        _scraper = scraper;
        _nemligClient = nemligClient;
        _scrapers = scrapers;
    }

    public string[] GetFilter()
    {
        return new[] { "basket", "delivery_select", "command" };
    }

    public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = default)
    {
        string value = message.ConvertPayloadToString();

        _logger.LogInformation("Updating delivery time to {NewTime}", value);

        await _scraper.SetValue(value, token);

        _logger.LogInformation("Updating basket");

        NemligBasket basket = await _nemligClient.GetBasket(token);
        await _scrapers.Process(basket, token);
    }
}