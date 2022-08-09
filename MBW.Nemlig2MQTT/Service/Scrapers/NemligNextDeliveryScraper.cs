using System;
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
using MBW.Nemlig2MQTT.Enums;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service.Helpers;
using Microsoft.Extensions.Logging;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligNextDeliveryScraper : IResponseScraper
{
    private readonly ILogger<NemligNextDeliveryScraper> _logger;
    private readonly NemligClient _nemligClient;
    private readonly HassMqttManager _hassMqttManager;
    private readonly DeliveryRenderer _deliveryRenderer;

    private readonly ISensorContainer _nextDeliveryTime;
    private readonly ISensorContainer _nextDeliveryContents;
    private readonly ISensorContainer _nextDeliveryBoxes;
    private readonly ISensorContainer _nextDeliveryEditDeadline;
    private readonly ISensorContainer _nextDeliveryOnTheWay;
    private readonly ISensorContainer _nextDeliveryEditDeadlinePassed;

    public NemligNextDeliveryScraper(
        ILogger<NemligNextDeliveryScraper> logger,
        NemligClient nemligClient,
        DeliveryRenderer deliveryRenderer,
        HassMqttManager hassMqttManager)
    {
        _logger = logger;
        _nemligClient = nemligClient;
        _deliveryRenderer=deliveryRenderer;
        _hassMqttManager = hassMqttManager;

        _nextDeliveryTime = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "timestamp")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Next delivery";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .GetSensor();

        _nextDeliveryBoxes = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "boxcount")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery boxes count";
            })
            .ConfigureAliveService()
            .GetSensor();

        _nextDeliveryContents = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "contents")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery contents";
            })
            .ConfigureAliveService()
            .GetSensor();

        _nextDeliveryEditDeadline = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery edit deadline";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .GetSensor();

        _nextDeliveryEditDeadlinePassed = _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline_passed")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery edit deadline passed";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;
            })
            .ConfigureAliveService()
            .GetSensor();

        _nextDeliveryOnTheWay = _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "ontheway")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery on the way";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Presence;
                discovery.PayloadOn = NemligDeliveryOnTheWay.Delivering.ToString();
                discovery.PayloadOff = NemligDeliveryOnTheWay.Idle.ToString();
            })
            .ConfigureAliveService()
            .GetSensor();
    }

    public async Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not BasicOrderHistory orderHistory)
            return;

        Order nextDeliveryOrder = orderHistory.Orders
            .Where(s => s.Status is OrderStatus.Bestilt or OrderStatus.Ekspederes || s.IsDeliveryOnWay)
            .MinBy(s => s.DeliveryTime.Start);

        if (nextDeliveryOrder != null)
        {
            OrderHistory orderDetails = await _nemligClient.GetOrderHistory(nextDeliveryOrder.Id, token);

            _logger.LogInformation("Next order {OrderId} is at {DeliveryTime}", nextDeliveryOrder.Id, nextDeliveryOrder.DeliveryTime.Start);

            Update(orderDetails);
        }
        else
        {
            // No next order
            Clear();
        }
    }

    private void Clear()
    {
        _nextDeliveryTime.SetValue(HassTopicKind.State, null);
        _nextDeliveryContents.SetValue(HassTopicKind.State, "");
        _nextDeliveryBoxes.SetValue(HassTopicKind.State, 0);
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, null);
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, NemligDeliveryOnTheWay.Idle.ToString());
        _nextDeliveryEditDeadlinePassed.SetValue(HassTopicKind.State, "off");
    }

    private void Update(OrderHistory orderDetails)
    {
        _nextDeliveryTime.SetValue(HassTopicKind.State, orderDetails.DeliveryTime.Start);
        _deliveryRenderer.RenderContents(_nextDeliveryContents, orderDetails.Lines);
        _nextDeliveryBoxes.SetValue(HassTopicKind.State, orderDetails.NumberOfPacks);
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, orderDetails.DeliveryDeadlineDateTime);

        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, (orderDetails.IsDeliveryOnWay ? NemligDeliveryOnTheWay.Delivering : NemligDeliveryOnTheWay.Idle).ToString());

        if (orderDetails.DeliveryDeadlineDateTimeOffset > DateTimeOffset.UtcNow)
            // Not passed
            _nextDeliveryEditDeadlinePassed.SetValue(HassTopicKind.State, "off");
        else
            _nextDeliveryEditDeadlinePassed.SetValue(HassTopicKind.State, "on");
    }
}