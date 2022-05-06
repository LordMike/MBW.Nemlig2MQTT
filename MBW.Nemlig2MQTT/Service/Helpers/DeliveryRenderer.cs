using System.Collections.Generic;
using System.Linq;
using MBW.Client.NemligCom.Objects.Delivery;

namespace MBW.Nemlig2MQTT.Service.Helpers
{
    public class DeliveryRenderer
    {
        public string Render(Dayhour dayHour)
        {
            string type = dayHour.Type == NemligDeliveryType.Attended ? "pers." : "fleks.";
            return $"{dayHour.Date:ddd dd'/'MM} kl.{dayHour.StartHour:00}-{dayHour.EndHour:00}, {dayHour.DeliveryPrice:#0,0}DKK ({type})";
        }

        public Dayhour FindByString(IEnumerable<Dayhour> source, string needle)
        {
            return source.FirstOrDefault(s => Render(s) == needle);
        }
    }
}