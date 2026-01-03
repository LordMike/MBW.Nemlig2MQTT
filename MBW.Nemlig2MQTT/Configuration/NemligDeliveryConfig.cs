using System;
using System.ComponentModel.DataAnnotations;
using MBW.Client.NemligCom.Objects.Delivery;

namespace MBW.Nemlig2MQTT.Configuration;

internal class NemligDeliveryConfig
{
    public NemligDeliveryType[] AllowDeliveryTypes { get; set; }


    [Range(typeof(TimeSpan), "00:01:00", "15.00:00:00")]
    public TimeSpan NextDeliveryCheckInterval { get; set; } = TimeSpan.FromHours(1);
}