using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace MBW.Nemlig2MQTT.Service
{
    internal class NemligBasketMqttService : BackgroundService
    {
        private static readonly Uri NemligBaseUrl = new Uri("https://www.nemlig.com/");

        private readonly ILogger<NemligBasketMqttService> _logger;
        private readonly NemligClient _nemligClient;
        private readonly HassMqttManager _hassMqttManager;
        private readonly ApiOperationalContainer _apiOperationalContainer;
        private readonly NemligConfiguration _config;
        private readonly AsyncAutoResetEvent _syncEvent = new AsyncAutoResetEvent();

        private ISensorContainer _basketBalance;
        private ISensorContainer _basketReadyToOrder;
        private ISensorContainer _basketDelivery;
        private ISensorContainer _basketContents;

        public NemligBasketMqttService(
            ILogger<NemligBasketMqttService> logger,
            IOptions<NemligConfiguration> config,
            NemligClient nemligClient,
            HassMqttManager hassMqttManager,
            ApiOperationalContainer apiOperationalContainer)
        {
            _logger = logger;
            _nemligClient = nemligClient;
            _hassMqttManager = hassMqttManager;
            _apiOperationalContainer = apiOperationalContainer;
            _config = config.Value;
        }

        public void ForceSync()
        {
            _syncEvent.Set();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateEntities();

            // Update loop
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Beginning update");

                try
                {
                    NemligBasket basket = await _nemligClient.GetBasket(stoppingToken);

                    Update(basket);

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
                    using CancellationTokenSource cts = new CancellationTokenSource(_config.BasketInterval);
                    using CancellationTokenSource combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

                    await _syncEvent.WaitAsync(combinedCancel.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Update(NemligBasket basket)
        {
            _basketBalance.SetValue(HassTopicKind.State, basket.TotalPrice);

            MqttAttributesTopic attr = _basketBalance.GetAttributesSender();
            attr.SetAttribute("num_bags", basket.NumberOfBags);
            attr.SetAttribute("num_deposits", basket.NumberOfDeposits);
            attr.SetAttribute("num_products", basket.NumberOfProducts);
            attr.SetAttribute("price_products", basket.TotalProductsPrice);
            attr.SetAttribute("price_delivery", basket.DeliveryPrice);
            attr.SetAttribute("price_bags", basket.TotalBagsPrice);
            attr.SetAttribute("price_deposits", basket.TotalDepositsPrice);
            attr.SetAttribute("price_nemligaccount", -basket.NemligAccount);
            attr.SetAttribute("price_ccfee", -basket.CreditCardFee);

            _basketDelivery.SetValue(HassTopicKind.State, basket.FormattedDeliveryTime);
            attr = _basketDelivery.GetAttributesSender();
            attr.SetAttribute("delivery", basket.FormattedDeliveryTime);
            attr.SetAttribute("delivery_price", basket.DeliveryPrice);
            attr.SetAttribute("delivery_date", basket.DeliveryTimeSlot.Date.ToString("yyyy-MM-dd"));
            attr.SetAttribute("delivery_time", $"{basket.DeliveryTimeSlot.StartTime:00}:00-{basket.DeliveryTimeSlot.EndTime:00}:00");

            if (basket.IsMinTotalValid)
                _basketReadyToOrder.SetValue(HassTopicKind.State, "ready");
            else
                _basketReadyToOrder.SetValue(HassTopicKind.State, "not_ready");

            string[] lines = basket.Lines.Select(s => $"{s.Quantity}x {s.Name} ({s.Price:#0.00} DKK)").ToArray();
            _basketContents.SetValue(HassTopicKind.State, lines);
            attr = _basketContents.GetAttributesSender();
            attr.Clear();

            for (int i = 0; i < basket.Lines.Length; i++)
            {
                Line line = basket.Lines[i];

                attr.SetAttribute($"line_{i}_id", line.Id);
                attr.SetAttribute($"line_{i}_name", line.Name);
                attr.SetAttribute($"line_{i}_quantity", line.Quantity);
                attr.SetAttribute($"line_{i}_description", line.Description);
                attr.SetAttribute($"line_{i}_image", line.PrimaryImage);
                attr.SetAttribute($"line_{i}_price", line.Price);
                attr.SetAttribute($"line_{i}_url", new Uri(NemligBaseUrl, line.Url).ToString());
            }
        }

        private void CreateEntities()
        {
            _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "balance")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Nemlig basket balance";
                    discovery.UnitOfMeasurement = "DKK";
                })
                .ConfigureAliveService();

            _basketBalance = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), "balance");

            _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Nemlig basket delivery";
                })
                .ConfigureAliveService();

            _basketDelivery = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery");

            _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "contents")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Nemlig basket";
                })
                .ConfigureAliveService();

            _basketContents = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), "contents");

            _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "ready")
                .ConfigureTopics(HassTopicKind.State)
                .ConfigureBasketDevice()
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Nemlig basket ready to order";
                    discovery.DeviceClass = HassDeviceClass.Problem;
                    discovery.PayloadOn = "not_ready";
                    discovery.PayloadOff = "ready";
                })
                .ConfigureAliveService();

            _basketReadyToOrder = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetBasketDeviceId(), "ready");
        }
    }
}