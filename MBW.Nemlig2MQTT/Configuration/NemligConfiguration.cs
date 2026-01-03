using System;
using System.ComponentModel.DataAnnotations;

namespace MBW.Nemlig2MQTT.Configuration;

internal class NemligConfiguration
{
    public string Username { get; set; }

    public string Password { get; set; }

    [Range(typeof(TimeSpan), "00:01:00", "15.00:00:00")]
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    [Range(typeof(TimeSpan), "00:01:00", "15.00:00:00")]
    public TimeSpan OrderHistoryCheckInterval { get; set; } = TimeSpan.FromHours(4);

    public NemligDeliveryConfig DeliveryConfig { get; set; } = new NemligDeliveryConfig();

    public bool EnableNextDelivery { get; set; } = true;
    public bool EnableOrderHistory { get; set; } = true;
}