using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.Client.NemligCom.DependencyInjection;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.HassMQTT.Topics;
using MBW.Nemlig2MQTT.Commands;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service;
using MBW.Nemlig2MQTT.Service.Helpers;
using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using WebProxy = System.Net.WebProxy;

namespace MBW.Nemlig2MQTT;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // Logging to use before logging configuration is read
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

        await CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddJsonFile("appsettings.local.json", true);

                string extraConfigFile = Environment.GetEnvironmentVariable("EXTRA_CONFIG_FILE");

                if (extraConfigFile != null)
                {
                    Log.Logger.Information("Loading extra config file at {path}", extraConfigFile);
                    builder.AddJsonFile(extraConfigFile, true);
                }
            })
            .ConfigureLogging((context, builder) =>
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration, "Logging")
                    .CreateLogger();

                builder
                    .ClearProviders()
                    .AddSerilog();
            })
            .ConfigureServices(ConfigureServices);

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services
            .AddAndConfigureMqtt("Nemlig2MQTT", configuration =>
            {
                NemligHassConfiguration nemligConfig = context.Configuration.GetSection("HASS").Get<NemligHassConfiguration>();
                configuration.SendDiscoveryDocuments = nemligConfig.EnableHASSDiscovery;
                configuration.ValidateDiscoveryDocuments = true;
            })
            .Configure<CommonMqttConfiguration>(x => x.ClientId = "nemlig2mqtt")
            .Configure<CommonMqttConfiguration>(context.Configuration.GetSection("MQTT"));

        // Commands
        services
            .AddMqttCommandService()
            .AddMqttCommandHandler<BasketSyncCommand>()
            .AddMqttCommandHandler<BasketOrderCcCommand>()
            .AddMqttCommandHandler<FullSyncCommand>()
            .AddMqttCommandHandler<DeliveryTimeSetCommand>();

        services
            .Configure<NemligHassConfiguration>(context.Configuration.GetSection("HASS"))
            .Configure<HassConfiguration>(context.Configuration.GetSection("HASS"))
            .Configure<NemligConfiguration>(context.Configuration.GetSection("Nemlig"))
            .PostConfigure<NemligConfiguration>(x => x.DeliveryConfig.AllowDeliveryTypes ??= new[] { NemligDeliveryType.Attended, NemligDeliveryType.Unattended })
            .Configure<ProxyConfiguration>(context.Configuration.GetSection("Proxy"))
            .AddSingleton(x => new HassMqttTopicBuilder(x.GetOptions<HassConfiguration>()))
            .AddSingleton<CookieContainer>()
            .AddHttpClient("nemlig")
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            }))
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                ProxyConfiguration proxyConfig = provider.GetOptions<ProxyConfiguration>();

                SocketsHttpHandler handler = new SocketsHttpHandler
                {
                    UseCookies = true,
                    CookieContainer = provider.GetRequiredService<CookieContainer>()
                };

                if (proxyConfig.Uri != null)
                    handler.Proxy = new WebProxy(proxyConfig.Uri);

                return handler;
            })
            .Services
            .AddNemligClient((provider, builder) =>
            {
                IHttpClientFactory httpFactory = provider.GetRequiredService<IHttpClientFactory>();
                NemligConfiguration config = provider.GetOptions<NemligConfiguration>();

                builder
                    .UseUsernamePassword(config.Username, config.Password)
                    .UseHttpClientFactory(httpFactory, "nemlig");
            });

        services
            .AddSingleton<DeliveryRenderer>()
            .AddSingletonAndHostedService<ApiOperationalContainer>()
            .AddSingletonAndHostedService<NemligMqttService>()
            .AddSingleton<ScraperManager>()
            .AddSingleton<NemligDeliveryOptionsScraper>()
            .AddSingleton<IEnumerable<IResponseScraper>>(provider =>
            {
                List<IResponseScraper> res = new List<IResponseScraper>();
                NemligConfiguration config = provider.GetOptions<NemligConfiguration>();

                if (config.EnableBasket)
                    res.Add(ActivatorUtilities.CreateInstance<NemligBasketContentsScraper>(provider));

                if (config.EnableDeliveryOptions)
                    // This is special, needs to be invoked by command also
                    res.Add(provider.GetRequiredService<NemligDeliveryOptionsScraper>());

                if (config.EnableNextDelivery)
                    res.Add(ActivatorUtilities.CreateInstance<NemligNextDeliveryScraper>(provider));

                if (config.EnableBuyBasket)
                    res.Add(ActivatorUtilities.CreateInstance<NemligCreditCardsScraper>(provider));

                return res;
            });
    }
}