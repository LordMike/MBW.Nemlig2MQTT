using System;

namespace MBW.Client.NemligCom.Objects.Delivery;

public class Dayhour
{
    public int Id { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int NumberOfHours { get; set; }
    public float DeliveryPrice { get; set; }
    public float? OriginalDeliveryPrice { get; set; }
    public bool IsOrHasVipVariantTimeSlot { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime Date { get; set; }
    public bool IsEventSlot { get; set; }
    public bool IsCheapHour { get; set; }
    public bool IsSelected { get; set; }
    public object[] Attributes { get; set; }
    public NemligDeliveryType Type { get; set; }
    public NemligDeliveryAvailability Availability { get; set; }
}