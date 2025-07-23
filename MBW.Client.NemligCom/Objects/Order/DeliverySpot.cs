using System;

namespace MBW.Client.NemligCom.Objects.Order;

public class DeliverySpot
{
    public string Id { get; set; }
    public string CustomerName { get; set; }
    public string OrderNumber { get; set; }
    public DeliverySpotState State { get; set; }
    public Deliverytime TimeSlot { get; set; }
    public DateTime EditDeadline { get; set; }
    public float Progress { get; set; }
    public DateTime DeliveryTime { get; set; }
    public Deliverytime DeliveryInterval { get; set; }
}
