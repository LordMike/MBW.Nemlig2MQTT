using System.Collections.Generic;
using System.Linq;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.Client.NemligCom.Objects.Shared;
using MBW.HassMQTT;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.Nemlig2MQTT.Service.Helpers;

public class DeliveryRenderer
{
    public string Render(Dayhour dayHour)
    {
        string type = dayHour.Type == NemligDeliveryType.Attended ? "pers." : "fleks.";
        return $"{dayHour.Date:ddd dd'/'MM} kl.{dayHour.StartHour:00}-{dayHour.EndHour:00}, {dayHour.DeliveryPrice:#0,0} DKK ({type})";
    }

    public Dayhour FindByString(IEnumerable<Dayhour> source, string needle)
    {
        return source.FirstOrDefault(s => Render(s) == needle);
    }

    public void RenderContents(ISensorContainer sensor, IEnumerable<INemligLine> products)
    {
        IList<INemligLine> productsList = products as IList<INemligLine> ?? products.ToList();

        string str = $"{productsList.Sum(s => s.Quantity)} stk";
        sensor.SetValue(HassTopicKind.State, str);

        MqttAttributesTopic attr = sensor.GetAttributesSender();

        string[] lines = productsList.Select(s => $"{s.Quantity}x {s.Name} ({s.Price:#0.00} DKK)").ToArray();
        attr.SetAttribute("contents", lines);
    }
}