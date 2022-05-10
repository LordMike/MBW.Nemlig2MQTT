using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Checkout;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.Client.NemligCom.Objects.Order;
using MBW.HassMQTT;
using MBW.Nemlig2MQTT.Configuration;
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
        _syncEvent.Set();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Update once
        {
            _logger.LogDebug("Updating credit cards, once");

            // Get credit cards
            NemligCreditCard[] creditCards = await _nemligClient.GetCreditCards(token: stoppingToken);
            await _scrapers.Process(creditCards, stoppingToken);
        }

        // Update loop
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Beginning update");

            try
            {
                // Get basket
                NemligBasket basket = await _nemligClient.GetBasket(stoppingToken);
                await _scrapers.Process(basket, stoppingToken);

                // Get delivery options
                NemligDeliveryDaysResponse deliveryOptions = await _nemligClient.GetDeliveryDays(_config.DeliveryConfig.DaysToCheck, stoppingToken);
                await _scrapers.Process(deliveryOptions, stoppingToken);

                // Get ongoing orders, we assume they're all on first page..
                BasicOrderHistory orderHistory = await _nemligClient.GetBasicOrderHistory(0, 10, stoppingToken);
                await _scrapers.Process(orderHistory, stoppingToken);

                // Track API operational status
                _apiOperationalContainer.MarkOk();

                await _hassMqttManager.FlushAll(stoppingToken);
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

            try
            {
                using CancellationTokenSource cts = new CancellationTokenSource(_config.CheckInterval);
                using CancellationTokenSource combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

                await _syncEvent.WaitAsync(combinedCancel.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}