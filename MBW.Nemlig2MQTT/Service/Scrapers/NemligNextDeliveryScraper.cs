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

    private ISensorContainer _nextDeliveryTime;
    private ISensorContainer _nextDeliveryContents;
    private ISensorContainer _nextDeliveryBoxes;
    private ISensorContainer _nextDeliveryEditDeadline;
    private ISensorContainer _nextDeliveryOnTheWay;

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
    
    public async Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not BasicOrderHistory orderHistory)
            return;

        Order nextDeliveryOrder = orderHistory.Orders
            .Where(s => s.Status != OrderStatus.Faktureret)
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
}