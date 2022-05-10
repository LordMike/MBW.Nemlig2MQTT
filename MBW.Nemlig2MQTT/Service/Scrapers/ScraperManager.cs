using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class ScraperManager
{
    private readonly IResponseScraper[] _scrapers;

    public ScraperManager(IEnumerable<IResponseScraper> scrapers)
    {
        _scrapers = scrapers.ToArray();
    }

    public async Task Process(object obj, CancellationToken token = default)
    {
        foreach (IResponseScraper scraper in _scrapers)
            await scraper.Scrape(obj, token);
    }
}