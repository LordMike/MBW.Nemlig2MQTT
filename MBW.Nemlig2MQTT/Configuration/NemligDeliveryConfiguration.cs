using MBW.Client.NemligCom.Objects.Delivery;
using System;
using System.ComponentModel.DataAnnotations;

namespace MBW.Nemlig2MQTT.Configuration;

internal class NemligDeliveryConfiguration
{
    [Range(1, 7)]
    public int DaysToCheck { get; set; } = 4;

    [Range(typeof(TimeSpan), "00:01:00", "15.00:00:00")]
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    [Range(4, 672)]
    public int PrioritizeMaxHours { get; set; } = 48;

    public bool PrioritizeCheapHours { get; set; } = true;

    public bool PrioritizeShortTimespan { get; set; } = false;

    public bool PrioritizeFreeDelivery { get; set; } = true;

    [Range(0, 23)]
    public byte[] PrioritizeHours { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxDeliveryPrice { get; set; }

    public NemligDeliveryType[] AllowDeliveryTypes { get; set; }


    [Range(typeof(TimeSpan), "00:01:00", "15.00:00:00")]
    public TimeSpan NextDeliveryCheckInterval { get; set; } = TimeSpan.FromHours(1);
}