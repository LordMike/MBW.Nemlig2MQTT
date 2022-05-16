using System;

namespace MBW.Client.NemligCom.Objects.Delivery;

public class Dayrangehour
{
    public Dayhour[] DayHours { get; set; }
    public bool IsAttendedAvailable { get; set; }
    public bool IsUnattendedAvailable { get; set; }
    public DateTime Date { get; set; }
    public bool IsCheapDay { get; set; }
    public string DeliveryText { get; set; }
    public bool IsSelected { get; set; }
}