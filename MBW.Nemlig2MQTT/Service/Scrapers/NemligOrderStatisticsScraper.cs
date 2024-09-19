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
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using Microsoft.Extensions.Logging;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligOrderStatisticsScraper : IResponseScraper
{
    private readonly ILogger<NemligOrderStatisticsScraper> _logger;
    private readonly HassMqttManager _hassMqttManager;

    private readonly ISensorContainer _pastMonthOrders;
    private readonly ISensorContainer _pastMonthOrderSum;
    private readonly ISensorContainer _pastQuarterOrders;
    private readonly ISensorContainer _pastQuarterOrderSum;

    public NemligOrderStatisticsScraper(
        ILogger<NemligOrderStatisticsScraper> logger,
        HassMqttManager hassMqttManager)
    {
        _logger = logger;
        _hassMqttManager = hassMqttManager;

        _pastMonthOrders = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetOrderStatisticsDeviceId(), "orders_1mo_count")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureOrderStatisticsDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Orders in the past 30 days"; })
            .ConfigureAliveService()
            .GetSensor();

        _pastMonthOrderSum = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetOrderStatisticsDeviceId(), "orders_1mo_sum")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureOrderStatisticsDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Orders total sum the past 30 days";
                discovery.UnitOfMeasurement = "DKK";
                discovery.DeviceClass = HassSensorDeviceClass.Monetary;
            })
            .ConfigureAliveService()
            .GetSensor();

        _pastQuarterOrders = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetOrderStatisticsDeviceId(), "orders_3mo_count")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureOrderStatisticsDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Orders in the past 90 days"; })
            .ConfigureAliveService()
            .GetSensor();

        _pastQuarterOrderSum = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetOrderStatisticsDeviceId(), "orders_3mo_sum")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureOrderStatisticsDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Orders total sum in the past 90 days";
                discovery.UnitOfMeasurement = "DKK";
                discovery.DeviceClass = HassSensorDeviceClass.Monetary;
            })
            .ConfigureAliveService()
            .GetSensor();
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not BasicOrderHistory orderHistory)
            return Task.CompletedTask;

        static void Process(IEnumerable<Order> orders, ISensorContainer ordersSensor, ISensorContainer sumSensor)
        {
            int count = 0;
            decimal sum = 0;
            DateTime? earliest = null, latest = null;

            foreach (Order order in orders)
            {
                count++;
                sum += (decimal)order.SubTotal;

                if (!earliest.HasValue || order.DeliveryTime.Start < earliest)
                    earliest = order.DeliveryTime.Start.UtcDateTime;
                if (!latest.HasValue || latest < order.DeliveryTime.Start)
                    latest = order.DeliveryTime.Start.UtcDateTime;
            }

            ordersSensor.SetValue(HassTopicKind.State, count);
            ordersSensor.SetAttribute("Earliest order", earliest);
            ordersSensor.SetAttribute("Latest order", latest);

            sumSensor.SetValue(HassTopicKind.State, sum);
            sumSensor.SetAttribute("Earliest order", earliest);
            sumSensor.SetAttribute("Latest order", latest);
        }

        DateTime bounds = DateTime.UtcNow.AddMonths(-1);
        Process(orderHistory.Orders.Where(s => s.DeliveryTime.Start >= bounds), _pastMonthOrders, _pastMonthOrderSum);

        bounds = DateTime.UtcNow.AddMonths(-3);
        Process(orderHistory.Orders.Where(s => s.DeliveryTime.Start >= bounds), _pastQuarterOrders, _pastQuarterOrderSum);

        if (orderHistory.Orders.Length > 0 && orderHistory.Orders.Last().DeliveryTime.Start > bounds)
        {
            // The latest order is within our bounds, this means we may hve more orders in Nemlig, for the time period, than we have in the order history object. We should warn on this.
            _logger.LogWarning("Order history of {Count} orders was not enough to fully assess the statistics for the past three months", orderHistory.Orders.Length);
        }

        _logger.LogInformation("Assessed {Count} orders for past 30/90 days stats", orderHistory.Orders.Length);
        return Task.CompletedTask;
    }
}