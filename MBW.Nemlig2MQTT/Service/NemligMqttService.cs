using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Checkout;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.Client.NemligCom.Objects.Order;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service.Scrapers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace MBW.Nemlig2MQTT.Service;

internal class NemligMqttService : BackgroundService
{
    private readonly ILogger<NemligMqttService> _logger;
    private readonly NemligClient _nemligClient;
    private readonly HassMqttManager _hassMqttManager;
    private readonly ApiOperationalContainer _apiOperationalContainer;
    private readonly ScraperManager _scrapers;
    private readonly AsyncAutoResetEvent _syncEvent = new AsyncAutoResetEvent();
    private readonly NemligConfiguration _config;

    private DateTime _lastOrderHistoryCheck = DateTime.MinValue;
    
    public NemligMqttService(
        ILogger<NemligMqttService> logger,
        IOptions<NemligConfiguration> config,
        NemligClient nemligClient,
        HassMqttManager hassMqttManager,
        ApiOperationalContainer apiOperationalContainer,
        ScraperManager scrapers)
    {
        _logger = logger;
        _nemligClient = nemligClient;
        _hassMqttManager = hassMqttManager;
        _apiOperationalContainer = apiOperationalContainer;
        _scrapers = scrapers;
        _config = config.Value;
    }

    public void ForceSync()
    {
        _lastOrderHistoryCheck = DateTime.MinValue;
        _syncEvent.Set();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Prepare force sync button
        {
            _hassMqttManager.ConfigureSensor<MqttButton>(HassUniqueIdBuilder.GetSystemDeviceId(), "force_sync")
                .ConfigureSystemDevice()
                .ConfigureDiscovery(discovery => { discovery.Name = "Nemlig force sync all"; })
                .ConfigureAliveService()
                .ConfigureTopics(HassTopicKind.Command)
                .GetSensor();
        }

        // Update once
        {
            _logger.LogDebug("Updating credit cards, once");

            if (_config.EnableBuyBasket)
            {
                // Get credit cards
                NemligCreditCard[] creditCards = await _nemligClient.GetCreditCards(token: stoppingToken);
                await _scrapers.Process(creditCards, stoppingToken);
            }
        }

        // Update loop
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Beginning update");

            TimeSpan nextWait = _config.CheckInterval;
            LatestOrderHistory latestOrder = null;

            try
            {
                if (_config.EnableBasket)
                {
                    // Get basket
                    NemligBasket basket = await _nemligClient.GetBasket(stoppingToken);
                    await _scrapers.Process(basket, stoppingToken);
                }

                if (_config.EnableDeliveryOptions)
                {
                    // Get delivery options
                    NemligDeliveryDaysResponse deliveryOptions = await _nemligClient.GetDeliveryDays(_config.DeliveryConfig.DaysToCheck, stoppingToken);
                    await _scrapers.Process(deliveryOptions, stoppingToken);
                }

                DeliverySpot deliverySpot;

                if (_config.EnableNextDelivery)
                {
                    latestOrder = await _nemligClient.GetLatestOrderHistory(stoppingToken);
                    await _scrapers.Process(latestOrder, stoppingToken);

                    if (latestOrder.Order != null &&
                        (latestOrder.Order.IsDeliveryOnWay || latestOrder.Order.Status == OrderStatus.Ekspederes ||
                         latestOrder.Order.DeliveryTime.Start - DateTimeOffset.UtcNow <= _config.DeliveryConfig.NextDeliveryCheckInterval))
                    {
                        try
                        {
                            deliverySpot = await _nemligClient.GetDeliverySpot(stoppingToken);
                            await _scrapers.Process(deliverySpot, stoppingToken);
                        }
                        catch (Exception e)
                        {
                            _logger.LogDebug(e, "Unable to retrieve DeliverySpot");
                        }
                    }
                }

                if (_config.EnableOrderHistory)
                {
                    // Should we dump orders?
                    DateTime nextDump = _lastOrderHistoryCheck.Add(_config.OrderHistoryCheckInterval);
                    if (nextDump < DateTime.UtcNow)
                    {
                        // Check
                        BasicOrderHistory orderHistory =  await _nemligClient.GetBasicOrderHistory(0, 20, stoppingToken);
                        await _scrapers.Process(orderHistory, stoppingToken);

                        _lastOrderHistoryCheck = DateTime.UtcNow;
                    }
                }

                // Track API operational status
                _apiOperationalContainer.MarkOk();

                await _hassMqttManager.FlushAll(stoppingToken);

                if (latestOrder?.Order != null)
                {
                    double minutes = (latestOrder.Order.DeliveryTime.Start - DateTimeOffset.UtcNow).TotalMinutes;
                    nextWait = minutes switch
                    {
                        var m when m <= 30 => TimeSpan.FromMinutes(5),
                        var m when m <= 240 => TimeSpan.FromMinutes(20),
                        var m when m <= _config.DeliveryConfig.NextDeliveryCheckInterval.TotalMinutes => _config.DeliveryConfig.NextDeliveryCheckInterval,
                        _ => _config.CheckInterval
                    };
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Do nothing
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while performing the update");

                // Track API operational status
                _apiOperationalContainer.MarkError(e.Message);
            }

            // Wait for next event, or the check interval
            try
            {
                using CancellationTokenSource cts = new CancellationTokenSource(nextWait);
                using CancellationTokenSource combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

                await _syncEvent.WaitAsync(combinedCancel.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}