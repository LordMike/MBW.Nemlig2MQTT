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
    private readonly DeliveryRenderer _deliveryRenderer;

    private int? _latestOrderId;
    private readonly IHassMqttEntity _nextDeliveryTime;
    private readonly IHassMqttEntity _nextDeliveryContents;
    private readonly IHassMqttEntity _nextDeliveryBoxes;
    private readonly IHassMqttEntity _nextDeliveryEditDeadline;
    private readonly IHassMqttEntity _nextDeliveryOnTheWay;
    private readonly IHassMqttEntity _nextDeliveryEditDeadlinePassed;
    private readonly IHassMqttEntity _nextDeliveryState;
    private readonly IHassMqttEntity _nextDeliveryEta;
    private readonly IHassMqttEntity _nextDeliveryEtaRangeMinutes;

    public NemligNextDeliveryScraper(
        ILogger<NemligNextDeliveryScraper> logger,
        NemligClient nemligClient,
        DeliveryRenderer deliveryRenderer,
        HassMqttManager hassMqttManager)
    {
        _logger = logger;
        _nemligClient = nemligClient;
        _deliveryRenderer = deliveryRenderer;

        _nextDeliveryTime = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Next delivery";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .PublishStateAndAttributesTogether()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "timestamp");

        _nextDeliveryBoxes = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Delivery boxes count"; })
            .ConfigureAliveService()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "boxcount");

        _nextDeliveryContents = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Delivery contents"; })
            .ConfigureAliveService()
            .PublishStateAndAttributesTogether()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "contents");

        _nextDeliveryEditDeadline = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery edit deadline";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline");

        _nextDeliveryEditDeadlinePassed = hassMqttManager.CreateEntity<MqttBinarySensor>()
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery edit deadline passed";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;
            })
            .ConfigureAliveService()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "editdeadline_passed");

        _nextDeliveryOnTheWay = hassMqttManager.CreateEntity<MqttBinarySensor>()
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
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "ontheway");

        _nextDeliveryState = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Delivery state"; })
            .ConfigureAliveService()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "delivery_state");

        _nextDeliveryEta = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery ETA";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .PublishStateAndAttributesTogether()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "delivery_eta");

        _nextDeliveryEtaRangeMinutes = hassMqttManager.CreateEntity<MqttSensor>()
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Delivery ETA range minutes";
                discovery.UnitOfMeasurement = "min";
            })
            .ConfigureAliveService()
            .Build(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "delivery_eta_range_minutes");
    }

    public async Task Scrape(object response, CancellationToken token = default)
    {
        if (response is DeliverySpot deliverySpot)
        {
            UpdateDeliverySpot(deliverySpot);
            return;
        }

        if (response is not LatestOrderHistory latestOrderHistory)
            return;

        if (latestOrderHistory.Order == null ||
            latestOrderHistory.Order.Status is not (OrderStatus.Bestilt or OrderStatus.Ekspederes) && !latestOrderHistory.Order.IsDeliveryOnWay)
        {
            // No next order
            Clear();
            return;
        }

        _logger.LogInformation("Next order {OrderId} is at {DeliveryTime}", latestOrderHistory.Order.Id, latestOrderHistory.Order.DeliveryTime.Start);

        await Update(latestOrderHistory.Order, token);
    }

    private void Clear()
    {
        _latestOrderId = null;
        _nextDeliveryTime.SetValue(HassTopicKind.State, MqttValue.Null);
        _nextDeliveryTime.GetAttributesSender().Clear();
        _nextDeliveryContents.SetValue(HassTopicKind.State, "");
        _nextDeliveryContents.GetAttributesSender().Clear();
        _nextDeliveryBoxes.SetValue(HassTopicKind.State, 0);
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, MqttValue.Null);
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, NemligDeliveryOnTheWay.Idle.ToString());
        _nextDeliveryEditDeadlinePassed.SetValue(HassTopicKind.State, "off");
        _nextDeliveryState.SetValue(HassTopicKind.State, DeliverySpotState.None.ToString());
        _nextDeliveryEta.SetValue(HassTopicKind.State, MqttValue.Null);
        _nextDeliveryEta.GetAttributesSender().Clear();
        _nextDeliveryEtaRangeMinutes.SetValue(HassTopicKind.State, MqttValue.Null);
    }

    private async Task Update(LatestOrderHistoryOrder order, CancellationToken token)
    {
        if (order.Id != _latestOrderId)
        {
            // Update order details
            // This does not change often, so we fetch it once per order
            OrderHistory orderDetails = await _nemligClient.GetOrderHistory(order.Id, token);

            _deliveryRenderer.RenderContents(_nextDeliveryContents, orderDetails.Lines);
            _nextDeliveryBoxes.SetValue(HassTopicKind.State, orderDetails.NumberOfPacks);

            _latestOrderId = order.Id;
        }

        _nextDeliveryTime.SetValue(HassTopicKind.State, order.DeliveryTime.Start);
        _nextDeliveryTime.SetAttribute("start", order.DeliveryTime.Start);
        _nextDeliveryTime.SetAttribute("end", order.DeliveryTime.End);
        _nextDeliveryEditDeadline.SetValue(HassTopicKind.State, order.DeliveryDeadlineDateTimeOffset);
        _nextDeliveryEditDeadlinePassed.SetValue(HassTopicKind.State, order.IsDeadlinePassed ? "on" : "off");
        _nextDeliveryState.SetValue(HassTopicKind.State, GetDeliveryState(order).ToString());
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, order.IsDeliveryOnWay ? nameof(NemligDeliveryOnTheWay.Delivering) : nameof(NemligDeliveryOnTheWay.Idle));

        if (order.IsDeliveryOnWay)
        {
            // DeliverySpot is the only source of actual in-flight ETA/range data.
            _nextDeliveryEta.SetValue(HassTopicKind.State, MqttValue.Null);
            _nextDeliveryEta.GetAttributesSender().Clear();
            _nextDeliveryEtaRangeMinutes.SetValue(HassTopicKind.State, MqttValue.Null);
        }
        else
        {
            _nextDeliveryEta.SetValue(HassTopicKind.State, order.DeliveryTime.Start);
            _nextDeliveryEta.SetAttribute("range_start", order.DeliveryTime.Start);
            _nextDeliveryEta.SetAttribute("range_end", order.DeliveryTime.End);
            _nextDeliveryEtaRangeMinutes.SetValue(HassTopicKind.State, (order.DeliveryTime.End - order.DeliveryTime.Start).TotalMinutes);
        }

    }

    private void UpdateDeliverySpot(DeliverySpot deliverySpot)
    {
        _nextDeliveryState.SetValue(HassTopicKind.State, deliverySpot.State.ToString());
        _nextDeliveryOnTheWay.SetValue(HassTopicKind.State, IsDeliveryInProgress(deliverySpot.State) ? nameof(NemligDeliveryOnTheWay.Delivering) : nameof(NemligDeliveryOnTheWay.Idle));
        _nextDeliveryEta.SetValue(HassTopicKind.State, deliverySpot.DeliveryTime);
        MqttAttributesTopic etaAttributes = _nextDeliveryEta.GetAttributesSender();
        
        if (deliverySpot.DeliveryInterval is not null)
        {
            etaAttributes.SetAttribute("range_start", deliverySpot.DeliveryInterval.Start);
            etaAttributes.SetAttribute("range_end", deliverySpot.DeliveryInterval.End);
            double rangeMinutes = (deliverySpot.DeliveryInterval.End - deliverySpot.DeliveryInterval.Start).TotalMinutes;
            _nextDeliveryEtaRangeMinutes.SetValue(HassTopicKind.State, rangeMinutes);
            return;
        }

        etaAttributes.RemoveAttribute("range_start");
        etaAttributes.RemoveAttribute("range_end");
        _nextDeliveryEtaRangeMinutes.SetValue(HassTopicKind.State, MqttValue.Null);
    }

    private static bool IsDeliveryInProgress(DeliverySpotState state)
    {
        return state is DeliverySpotState.Packing
            or DeliverySpotState.ReadyForDelivery
            or DeliverySpotState.OngoingDelivery;
    }

    private static DeliverySpotState GetDeliveryState(LatestOrderHistoryOrder order)
    {
        if (order.IsDeliveryOnWay)
            return DeliverySpotState.OngoingDelivery;

        return order.Status switch
        {
            OrderStatus.Bestilt => DeliverySpotState.Placed,
            OrderStatus.Ekspederes => DeliverySpotState.Packing,
            _ => DeliverySpotState.None
        };
    }

    // Estimate updates are provided by the DeliverySpot scraper
}
