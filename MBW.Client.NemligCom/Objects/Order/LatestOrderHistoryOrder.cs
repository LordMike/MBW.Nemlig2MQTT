using System;
using MBW.Client.NemligCom.Converters;
using Newtonsoft.Json;

namespace MBW.Client.NemligCom.Objects.Order;

public class LatestOrderHistoryOrder : Order
{
    [JsonConverter(typeof(OptionalTimezoneDateTimeConverter), "Europe/Copenhagen")]
    public DateTimeOffset? EstimatedArrivalTime { get; set; }
    // public object ActualArrivalTime { get; set; }
    // public object Buttons { get; set; }
    // public object ValidationFailures { get; set; }
}