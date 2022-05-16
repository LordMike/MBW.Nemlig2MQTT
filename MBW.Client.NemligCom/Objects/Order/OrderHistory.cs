using System;

namespace MBW.Client.NemligCom.Objects.Order;

public class OrderHistory
{
    public OrderHistoryLine[] Lines { get; set; }
    //public object[] CouponLines { get; set; }
    public string Notes { get; set; }
    public string UnattendedNotes { get; set; }
    public string PlacementMessage { get; set; }
    public string DoorCode { get; set; }
    public bool IsEkeyApplied { get; set; }
    public string KVHX { get; set; }
    public string DeliveryDate { get; set; }
    public string DeliveryDeadline { get; set; }
    public DateTime DeliveryDeadlineDateTime { get; set; }
    public float DeliveryDeadlineDateTimezoneOffset { get; set; }
    public Deliverytime DeliveryTime { get; set; }
    public int DeliveryType { get; set; }
    public int TimeslotDuration { get; set; }
    public string DebitorId { get; set; }
    public float SubTotal { get; set; }
    public float DepositPrice { get; set; }
    public float ShippingPrice { get; set; }
    public float PackagingPrice { get; set; }
    public float TransactionFee { get; set; }
    public float TotalVatAmount { get; set; }
    public float Bonus { get; set; }
    public float AddedToAccount { get; set; }
    public float CouponDiscount { get; set; }
    public float TotalProductDiscountPrice { get; set; }
    public int Id { get; set; }
    //public object SubscriptionId { get; set; }
    public string OrderNumber { get; set; }
    public string OrderDate { get; set; }
    public float Total { get; set; }
    public string Email { get; set; }
    public int Status { get; set; }
    public int NumberOfProducts { get; set; }
    public int NumberOfPacks { get; set; }
    public int NumberOfDeposits { get; set; }
    public float TotalProductDiscount { get; set; }
    public int DeliveryStatus { get; set; }
    //public object TranslationsJson { get; set; }
    public bool IsDeliveryOnWay { get; set; }
    public bool IsEditable { get; set; }
    public bool IsCancellable { get; set; }
    public bool HasInvoice { get; set; }
    public bool IsMinimumOrderTotalAchieved { get; set; }

    public DateTimeOffset DeliveryDeadlineDateTimeOffset => new(DeliveryDeadlineDateTime,
        TimeSpan.FromMinutes(DeliveryDeadlineDateTimezoneOffset));
}