namespace MBW.Client.NemligCom.Objects.Order;

public class NemligOrderLoggedInRequest
{
    public string Notes { get; set; }
    public string UnattendedNotes { get; set; }
    public string PlacementMessage { get; set; }
    public string DoorCode { get; set; }
    public string Password { get; set; }
    public bool ReturnOfBottlesRequested { get; set; }
    public bool AcceptMinimumAge { get; set; } = true;
    public bool TermsAndConditionsAccepted { get; set; } = true;
    public bool CheckForOrdersToMerge { get; set; } = true;
    public int PaymentCard { get; set; }
    public bool UseMobilePay { get; set; }
    public Emailsubscriptions EmailSubscriptions { get; set; }
    public bool HasNewsLetterWithOffersSubscription { get; set; }
    public bool HasNewsLetterWithMealplansSubscription { get; set; }
}