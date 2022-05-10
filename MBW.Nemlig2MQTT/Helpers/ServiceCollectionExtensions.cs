using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.DependencyInjection;

namespace MBW.Nemlig2MQTT.Helpers;

static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScraper<TScraper>(this IServiceCollection services) where TScraper : class, IResponseScraper
    {
        return services.AddSingleton<TScraper>().AddSingleton<IResponseScraper>(x => x.GetRequiredService<TScraper>());
    }
}