using System;

namespace MBW.Client.NemligCom.Objects.Delivery;

public class NemligDeliveryDaysResponse
{
    public Dayrangehour[] DayRangeHours { get; set; }
    public bool AnyVisibleAttendedInDeliveryZone { get; set; }
    public bool AnyVisibleUnattendedInDeliveryZone { get; set; }
    public float AttendedMinDeliveryPrice { get; set; }
    public float UnattendedMinDeliveryPrice { get; set; }
    public int SelectedTimeSlotId { get; set; }
    public DateTime SelectedDeliveryTime { get; set; }
    public bool IsTimeSlotReserved { get; set; }
    public object DeliveryText { get; set; }
    public bool IsReservationLost { get; set; }
    public object PostalDistrictCode { get; set; }
    public bool IsWholeDeliveryAvailable { get; set; }
    public object[] Messages { get; set; }
    public DateTime NextRangeStart { get; set; }
}