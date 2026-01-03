using System;

namespace MBW.Client.NemligCom.Objects.Order;

public class DeliverySpot
{
    public string Id { get; set; }
    public string CustomerName { get; set; }
    public string OrderNumber { get; set; }
    public DeliverySpotState State { get; set; }
    public Deliverytime TimeSlot { get; set; }
    public DateTimeOffset EditDeadline { get; set; }
    public float Progress { get; set; }
    public DateTimeOffset DeliveryTime { get; set; }
    public Deliverytime DeliveryInterval { get; set; }
}
