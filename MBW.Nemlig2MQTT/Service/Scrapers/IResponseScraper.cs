using System.Threading;
using System.Threading.Tasks;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal interface IResponseScraper
{
    Task Scrape(object response, CancellationToken token = default);
}