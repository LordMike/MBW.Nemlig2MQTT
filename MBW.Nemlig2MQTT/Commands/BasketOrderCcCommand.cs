using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using Newtonsoft.Json;

namespace MBW.Nemlig2MQTT.Commands;

internal class BasketOrderCcCommand : IMqttCommandHandler
{
    private readonly string _nemligPassword;
    private readonly ILogger<BasketOrderCcCommand> _logger;
    private readonly NemligClient _nemligClient;
    private readonly ScraperManager _scrapers;

    public BasketOrderCcCommand(ILogger<BasketOrderCcCommand> logger, IOptions<NemligConfiguration> configuration, NemligClient nemligClient, ScraperManager scrapers)
    {
        _nemligPassword = configuration.Value.Password;
        _logger = logger;
        _nemligClient = nemligClient;
        _scrapers = scrapers;
    }

    public string[] GetFilter()
    {
        return new[] { HassUniqueIdBuilder.GetBasketDeviceId(), "order-cc", null };
    }

    public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = default)
    {
        int ccId = int.Parse(topicLevels[2]);
        string request = message.ConvertPayloadToString();

        _logger.LogInformation("Ordering with card {CardId}", ccId);

        RequestDetails requestDetails;
        if (!string.IsNullOrEmpty(request) && request != "PRESS")
            requestDetails = JsonConvert.DeserializeObject<RequestDetails>(request)!;
        else
            requestDetails = new RequestDetails();

        requestDetails.PlacementMessage ??= "Frontdoor";

        await _nemligClient.OrderBasketWithCreditCard(ccId, _nemligPassword, requestDetails.PlacementMessage, token);

        _logger.LogInformation("Updating basket");

        NemligBasket basket = await _nemligClient.GetBasket(token);
        await _scrapers.Process(basket, token);
    }

    class RequestDetails
    {
        public string PlacementMessage { get; set; }
    }
}