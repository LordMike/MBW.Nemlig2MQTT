namespace MBW.Client.NemligCom.Objects.Delivery;

public class TryUpdateDeliveryTimeResponse
{
    public float PriceChangeDiff { get; set; }
    public int MinutesReserved { get; set; }
    public bool IsReserved { get; set; }
    //public object[] ProductLineDiffs { get; set; }
    //public object[] BundleLineDiffs { get; set; }
    //public object[] CouponLineDiffs { get; set; }
    public bool IsPriceDiffChangePositive { get; set; }
    public bool ShowDeadlineAlert { get; set; }
    public int MinutesTillDeadline { get; set; }
    public string TimeslotUtc { get; set; }
    public int DeliveryZoneId { get; set; }
    //public object MealBoxesValidationData { get; set; }
}