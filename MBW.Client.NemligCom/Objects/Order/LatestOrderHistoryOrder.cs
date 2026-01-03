using System;

namespace MBW.Client.NemligCom.Objects.Order;

public class LatestOrderHistoryOrder : Order
{
    public DateTimeOffset? EstimatedArrivalTime { get; set; }
    // public object ActualArrivalTime { get; set; }
    // public object Buttons { get; set; }
    // public object ValidationFailures { get; set; }
}
