using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Order;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.Interfaces;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Helpers;

namespace MBW.Nemlig2MQTT.Service.Scrapers;

internal class NemligDeliverySpotScraper : IResponseScraper
{
    private readonly ISensorContainer _nextDeliveryTimeEstimate;

    public NemligDeliverySpotScraper(HassMqttManager hassMqttManager)
    {
        _nextDeliveryTimeEstimate = hassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetNextDeliveryDeviceId(), "estimate")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureNextDeliveryDevice()
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Next delivery estimate";
                discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
            })
            .ConfigureAliveService()
            .GetSensor();
    }

    public Task Scrape(object response, CancellationToken token = default)
    {
        if (response is not DeliverySpot spot)
            return Task.CompletedTask;

        if (spot.DeliveryTime != default)
            _nextDeliveryTimeEstimate.SetValue(HassTopicKind.State, spot.DeliveryTime);
        else
            _nextDeliveryTimeEstimate.SetValue(HassTopicKind.State, null);

        if (spot.DeliveryInterval != null)
        {
            _nextDeliveryTimeEstimate.SetAttribute("start", spot.DeliveryInterval.Start);
            _nextDeliveryTimeEstimate.SetAttribute("end", spot.DeliveryInterval.End);
        }

        return Task.CompletedTask;
    }
}

