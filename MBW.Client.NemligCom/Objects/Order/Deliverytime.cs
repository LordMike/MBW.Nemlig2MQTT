using System;
using System.ComponentModel;
using MBW.Client.NemligCom.Converters;
using Newtonsoft.Json;

namespace MBW.Client.NemligCom.Objects.Order;

public class Deliverytime
{
    [JsonConverter(typeof(TimezoneDateTimeConverter), "Europe/Copenhagen")]
    public DateTimeOffset Start { get; set; }

    [JsonConverter(typeof(TimezoneDateTimeConverter), "Europe/Copenhagen")]
    public DateTimeOffset End { get; set; }
}