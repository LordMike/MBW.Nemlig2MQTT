using System;

namespace MBW.Nemlig2MQTT.Configuration;

internal class NemligConfiguration
{
    public string Username { get; set; }

    public string Password { get; set; }

    public TimeSpan BasketInterval { get; set; } = TimeSpan.FromHours(1);
}