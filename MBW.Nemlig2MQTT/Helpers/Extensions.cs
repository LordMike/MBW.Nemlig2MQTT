using System;
using MBW.HassMQTT.DiscoveryModels;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.HASS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.Nemlig2MQTT.Helpers
{
    internal static class Extensions
    {
        public static TOptions GetOptions<TOptions>(this IServiceProvider provider) where TOptions : class, new()
        {
            return provider.GetRequiredService<IOptions<TOptions>>().Value;
        }

        public static ILogger<T> GetLogger<T>(this IServiceProvider provider)
        {
            return provider.GetRequiredService<ILogger<T>>();
        }

        public static ILogger GetLogger(this IServiceProvider provider, Type type)
        {
            return provider.GetRequiredService<ILoggerFactory>().CreateLogger(type);
        }

        public static IServiceCollection AddSingletonAndHostedService<TService>(this IServiceCollection services) where TService : class, IHostedService
        {
            return services.AddSingleton<TService>()
                .AddHostedService(x => x.GetRequiredService<TService>());
        }

        public static IDiscoveryDocumentBuilder<T> ConfigureBasketDevice<T>(this IDiscoveryDocumentBuilder<T> builder) where T : MqttSensorDiscoveryBase
        {
            return builder.ConfigureDevice(device =>
            {
                device.Name = "Nemlig Basket";
                device.Identifiers = new[] {HassUniqueIdBuilder.GetBasketDeviceId()};
            });
        }
    }
}