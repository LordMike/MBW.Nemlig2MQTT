namespace MBW.Client.NemligCom.Objects.Basket;

public class NemligBasket
{
    public string BasketGuid { get; set; }
    public object Id { get; set; }
    public object OrderNumber { get; set; }
    public object PreviousOrderNumber { get; set; }
    public string PreviousBasketGuid { get; set; }
    public int PreviousBasketId { get; set; }
    public Invoiceaddress InvoiceAddress { get; set; }
    public Deliveryaddress DeliveryAddress { get; set; }
    public bool AddressesAreEqual { get; set; }
    public object[] Recipes { get; set; }
    public BasketLine[] Lines { get; set; }
    public object[] Coupons { get; set; }
    public object[] MealBoxes { get; set; }
    public Deliverytimeslot DeliveryTimeSlot { get; set; }
    public string Email { get; set; }
    public int NumberOfProducts { get; set; }
    public int NumberOfBags { get; set; }
    public int NumberOfDeposits { get; set; }
    public float CouponDiscount { get; set; }
    public string FormattedDeliveryTime { get; set; }
    public string Notes { get; set; }
    public string UnattendedNotes { get; set; }
    public string PlacementMessage { get; set; }
    public string DoorCode { get; set; }
    public float TotalProductsPrice { get; set; }
    public float TotalBagsPrice { get; set; }
    public float TotalDepositsPrice { get; set; }
    public float DeliveryPrice { get; set; }
    public float TotalProductDiscountPrice { get; set; }
    public float NemligAccount { get; set; }
    public float CreditCardFee { get; set; }
    public int CreditCardId { get; set; }
    public float TotalPrice { get; set; }
    public float TotalPriceWithoutFee { get; set; }
    public bool IsMinTotalValid { get; set; }
    public int MinimumOrderTotal { get; set; }
    public bool IsMaxTotalValid { get; set; }
    public float MaximumOrderTotal { get; set; }
    public object MinimumAgeRequired { get; set; }
    public int OrderStepRequired { get; set; }
    public object[] OrdersToMergeWith { get; set; }
    public object[] ValidationFailures { get; set; }
    public int PaymentMethod { get; set; }
}