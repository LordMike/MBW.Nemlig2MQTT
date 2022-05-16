namespace MBW.Client.NemligCom.Objects.Delivery;

public class NemligCheckPostalCodeResponse
{
    public int PostalDistrictCode { get; set; }
    public string PostalDistrictName { get; set; }
    public bool IsFullDeliverable { get; set; }
    public bool IsPartlyDeliverable { get; set; }
    public object[] DeliverableAddresses { get; set; }
}