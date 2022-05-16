using System;

namespace MBW.Client.NemligCom.Objects.Basket;

public class Deliverytimeslot
{
    public string Id { get; set; }
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public DateTime Date { get; set; }
    public int Duration { get; set; }
    public bool Reserved { get; set; }
    public bool ReservationLost { get; set; }
    public object[] Attributes { get; set; }
    public int DeliveryType { get; set; }
}