using System;
using System.Collections.Generic;
using System.Linq;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Delivery;
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

        string str = $"{products.Sum(s => s.Quantity)} stk";
        sensor.SetValue(HassTopicKind.State, str);

        MqttAttributesTopic attr = sensor.GetAttributesSender();

        string[] lines = productsList.Select(s => $"{s.Quantity}x {s.Name} ({s.Price:#0.00} DKK)").ToArray();
        attr.SetAttribute("contents", lines);

        //attr.SetAttribute("lines", productsList.Select(s => new LineItem(
        //    s.Id,
        //    s.Name,
        //    s.Description,
        //    s.Quantity,
        //    s.Price
        //)));

        //attr.SetAttribute($"line_{i}_image", line.PrimaryImage);
        //attr.SetAttribute($"line_{i}_url", new Uri(_nemligClient.NemligUrl, line.Url).ToString());
    }

    class LineItem : IComparable<LineItem>
    {
        public LineItem(int id, string name, string description, int quantity, float price)
        {
            Id = id;
            Name = name;
            Description = description;
            Quantity = quantity;
            Price = price;
        }

        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Quantity { get; }
        public float Price { get; }

        public int CompareTo(LineItem other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            int idComparison = Id.CompareTo(other.Id);
            if (idComparison != 0) return idComparison;
            int nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;
            int descriptionComparison = string.Compare(Description, other.Description, StringComparison.Ordinal);
            if (descriptionComparison != 0) return descriptionComparison;
            int quantityComparison = Quantity.CompareTo(other.Quantity);
            if (quantityComparison != 0) return quantityComparison;
            return Price.CompareTo(other.Price);
        }
    }
}