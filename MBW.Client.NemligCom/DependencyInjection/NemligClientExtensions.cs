using System;
using System.Net.Http;
using MBW.Client.NemligCom.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace MBW.Client.NemligCom.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class NemligClientExtensions
{
    public static IServiceCollection AddNemligClient(this IServiceCollection services, Action<NemligClientBuilder> configure)
    {
        return services.AddNemligClient((provider, builder) => configure(builder));
    }

    public static IServiceCollection AddNemligClient(this IServiceCollection services, Action<IServiceProvider, NemligClientBuilder> configure)
    {
        return services
            .AddHttpClient()
            .AddSingleton(x =>
            {
                NemligClientBuilder builder = new NemligClientBuilder()
                    .UseLogger(x.GetRequiredService<ILogger<NemligClient>>())
                    .UseHttpClientFactory(x.GetRequiredService<IHttpClientFactory>(), string.Empty);

                configure(x, builder);

                return builder.Build();
            });
    }
}