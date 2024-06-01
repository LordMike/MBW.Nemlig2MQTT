using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;
using MBW.Nemlig2MQTT.Service.Helpers;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligBasketContentsScraper : IResponseScraper
{
    private readonly DeliveryRenderer _deliveryRenderer;
    private readonly HassMqttManager _hassMqttManager;

    private readonly ISensorContainer _basketBalance;
    private readonly ISensorContainer _basketReadyToOrder;
    private readonly ISensorContainer _basketDelivery;
    private readonly ISensorContainer _basketContents;

    public NemligBasketContentsScraper(HassMqttManager hassMqttManager, DeliveryRenderer deliveryRenderer)
    {
        _deliveryRenderer = deliveryRenderer;
        _hassMqttManager = hassMqttManager;

        _basketBalance = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "balance")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Nemlig basket balance";
                discovery.UnitOfMeasurement = "DKK";
            })
            .ConfigureAliveService()
            .GetSensor();

        _basketDelivery = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "delivery")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Nemlig basket delivery"; })
            .ConfigureAliveService()
            .GetSensor();

        _basketContents = _hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "contents")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Nemlig basket"; })
            .ConfigureAliveService()
            .GetSensor();

        _basketReadyToOrder = _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetBasketDeviceId(), "ready")
            .ConfigureTopics(HassTopicKind.State)
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Nemlig basket ready to order";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;
                discovery.PayloadOn = "not_ready";
                discovery.PayloadOff = "ready";
            })
            .ConfigureAliveService()
            .GetSensor();


        // Create sync button
        _hassMqttManager.ConfigureSensor<MqttButton>(HassUniqueIdBuilder.GetBasketDeviceId(), "force_sync")
            .ConfigureBasketDevice()
            .ConfigureDiscovery(discovery => { discovery.Name = "Nemlig force sync basket"; })
            .ConfigureAliveService()
            .ConfigureTopics(HassTopicKind.Command)
            .GetSensor();
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not NemligBasket basket)
            return Task.CompletedTask;

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

        _deliveryRenderer.RenderContents(_basketContents, basket.Lines);

        return Task.CompletedTask;
    }
}