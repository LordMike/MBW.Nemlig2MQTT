using System;
using System.Diagnostics;

namespace MBW.Client.NemligCom.Objects.Order;

[DebuggerDisplay("Time: {DeliveryTime.Start}, {Status}, {DeliveryStatus}")]
public class Order
{
    public OrderStatus Status { get; set; }
    public string OrderNumber { get; set; }
    public float Total { get; set; }
    public float SubTotal { get; set; }
    public string OrderDate { get; set; }
    public int Id { get; set; }
    //public object SubscriptionId { get; set; }
    //public object Buttons { get; set; }
    public int DeliveryStatus { get; set; }
    public string DeliveryDeadlineDate { get; set; }
    public float DeliveryDeadlineDateTimezoneOffset { get; set; }
    public DateTime DeliveryDeadlineDateTime { get; set; }
    public string DeliveryAddress { get; set; }
    public bool IsDeliveryOnWay { get; set; }
    public Deliverytime DeliveryTime { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public int?[] TimeSlotAttributesTypes { get; set; }
    //public object ValidationFailures { get; set; }
    public bool IsEditable { get; set; }
    public bool IsCancellable { get; set; }
    public bool HasInvoice { get; set; }
    public bool IsMinimumOrderTotalAchieved { get; set; }
    public bool IsDeadlinePassed { get; set; }
}