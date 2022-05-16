namespace MBW.Client.NemligCom.Objects.Login;

public class WebApiLoginResponse
{
    public string RedirectUrl { get; set; }
    public bool MergeSuccessful { get; set; }
    public bool ZipCodeDiffers { get; set; }
    public string TimeslotUtc { get; set; }
    public int DeliveryZoneId { get; set; }
    public Gdprsettings GdprSettings { get; set; }
    public bool IsExternalLogin { get; set; }
    public bool IsFirstLogin { get; set; }
}