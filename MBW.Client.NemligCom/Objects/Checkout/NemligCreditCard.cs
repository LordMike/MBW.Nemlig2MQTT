using System;

namespace MBW.Client.NemligCom.Objects.Checkout;

public class NemligCreditCard
{
    public int CardId { get; set; }
    public string ExternalId { get; set; }
    public DateTime CardExpirationInfo { get; set; }
    public string CardExpirationMonth { get; set; }
    public string CardExpirationYear { get; set; }
    public string CardMask { get; set; }
    public string CardType { get; set; }
    public float FeeInPercent { get; set; }
    public bool IsDefault { get; set; }
}