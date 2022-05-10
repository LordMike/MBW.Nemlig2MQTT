using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Order;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.Enums;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace MBW.Nemlig2MQTT.Service;

internal class NemligNextDeliveryMqttService : BackgroundService
{
    private readonly ILogger<NemligNextDeliveryMqttService> _logger;
    private readonly NemligClient _nemligClient;
    private readonly HassMqttManager _hassMqttManager;
    private readonly ApiOperationalContainer _apiOperationalContainer;
    private readonly AsyncAutoResetEvent _syncEvent = new AsyncAutoResetEvent();
    private readonly NemligDeliveryConfiguration _config;
    private readonly Dictionary<string, int> _callbackLookup = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly DeliveryRenderer _deliveryRenderer;

    private ISensorContainer _nextDeliveryTime;
    private ISensorContainer _nextDeliveryContents;
    private ISensorContainer _nextDeliveryBoxes;
    private ISensorContainer _nextDeliveryEditDeadline;
    private ISensorContainer _nextDeliveryOnTheWay;

    public NemligNextDeliveryMqttService(
        ILogger<NemligNextDeliveryMqttService> logger,
        IOptions<NemligDeliveryConfiguration> config,
        NemligClient nemligClient,
        DeliveryRenderer deliveryRenderer,
        HassMqttManager hassMqttManager,
        ApiOperationalContainer apiOperationalContainer)
    {
        _logger = logger;
        _nemligClient = nemligClient;
        _deliveryRenderer=deliveryRenderer;
        _hassMqttManager = hassMqttManager;
        _apiOperationalContainer = apiOperationalContainer;
        _config = config.Value;
    }

    public void ForceSync()
    {
        _syncEvent.Set();
    }

    public async Task SetValue(string chosenValue, CancellationToken token = default)
    {
        _callbackLookup.TryGetValue(chosenValue, out var id);

        await _nemligClient.TryUpdateDeliveryTime(id, token);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CreateEntities();

        TimeSpan nextCheck = _config.NextDeliveryCheckInterval;

        // Update loop
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Beginning update");

            try
            {
                // Get ongoing orders, we assume they're all on first page..
                BasicOrderHistory orderHistory = await _nemligClient.GetBasicOrderHistory(0, 10, stoppingToken);
                var nextDeliveryOrder = orderHistory.Orders
                    .Where(s => s.Status != OrderStatus.Faktureret)
                    .OrderBy(s => s.DeliveryTime.Start)
                    .FirstOrDefault();

                if (nextDeliveryOrder != null)
                {
                    OrderHistory orderDetails = await _nemligClient.GetOrderHistory(nextDeliveryOrder.Id, stoppingToken);

                    Update(orderDetails);

                    nextCheck = _config.NextDeliveryCheckInterval / 4;
                }
                else
                {
                    // No next order
                    Clear();

                    nextCheck = _config.NextDeliveryCheckInterval;
                }

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
                using CancellationTokenSource cts = new CancellationTokenSource(nextCheck);
                using CancellationTokenSource combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

                await _syncEvent.WaitAsync(combinedCancel.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private void Clear()
    {
        _nextDeliveryTime.SetValue(HassTopicKind.State, "");
        _nextDeliveryContents.SetValue(HassTopicKind.State, "");
        _nextDeliveryBoxes.SetValue(HassTopicKind.State, "");
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, "");
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, NemligDeliveryOnTheWay.Idle.ToString());
    }

    private void Update(OrderHistory orderDetails)
    {
        _nextDeliveryTime.SetValue(HassTopicKind.State, orderDetails.DeliveryTime.Start.ToString("O"));
        _deliveryRenderer.RenderContents(_nextDeliveryContents, orderDetails.Lines);
        _nextDeliveryBoxes.SetValue(HassTopicKind.State, orderDetails.NumberOfPacks);
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, orderDetails.DeliveryDeadlineDateTime.ToString("O"));
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, NemligDeliveryOnTheWay.Idle.ToString());
    }

    private void CreateEntities()
    {
        _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "timestamp")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Next delivery";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService();

        _nextDeliveryTime = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "timestamp");

        _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "boxcount")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery boxes count";
            })
            .ConfigureAliveService();

        _nextDeliveryBoxes = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "boxcount");

        _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "contents")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery contents";
            })
            .ConfigureAliveService();

        _nextDeliveryContents = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "contents");

        _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery edit deadline";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService();

        _nextDeliveryEditDeadline = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline");

        _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "ontheway")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery on the way";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Presence;
                discovery.PayloadOn = NemligDeliveryOnTheWay.Delivering.ToString();
                discovery.PayloadOff = NemligDeliveryOnTheWay.Idle.ToString();
            })
            .ConfigureAliveService();

        _nextDeliveryOnTheWay = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "ontheway");
    }
}