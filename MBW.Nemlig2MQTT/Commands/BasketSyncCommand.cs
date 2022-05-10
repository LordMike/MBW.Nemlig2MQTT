using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands;

internal class BasketSyncCommand : IMqttCommandHandler
{
    private readonly ILogger<BasketSyncCommand> _logger;
    private readonly NemligClient _nemligClient;
    private readonly ScraperManager _scrapers;

    public BasketSyncCommand(ILogger<BasketSyncCommand> logger, NemligClient nemligClient, ScraperManager scrapers)
    {
        _logger = logger;
        _nemligClient = nemligClient;
        _scrapers = scrapers;
    }

    public string[] GetFilter()
    {
        return new[] { "basket", "sync" };
    }

    public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = default)
    {
        _logger.LogInformation("Updating basket");

        NemligBasket basket = await _nemligClient.GetBasket(token);
        await _scrapers.Process(basket, token);
    }
}