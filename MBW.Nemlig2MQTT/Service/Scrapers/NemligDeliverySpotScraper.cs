using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Order;
using Microsoft.Extensions.Logging;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligDeliverySpotScraper : IResponseScraper
{
    private readonly ILogger<NemligDeliverySpotScraper> _logger;
    private readonly NemligNextDeliveryScraper _nextDeliveryScraper;

    public NemligDeliverySpotScraper(ILogger<NemligDeliverySpotScraper> logger, NemligNextDeliveryScraper nextDeliveryScraper)
    {
        _logger = logger;
        _nextDeliveryScraper = nextDeliveryScraper;
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not DeliverySpot spot)
            return Task.CompletedTask;

        _logger.LogInformation("Delivery spot update, estimated arrival {Arrival}", spot.DeliveryTime);
        _nextDeliveryScraper.SetEstimatedArrival(spot.DeliveryTime);

        return Task.CompletedTask;
    }
}
