namespace MBW.Client.NemligCom.Objects.Settings;

public class Sitelogossettings
{
    public string Id { get; set; }
    public Medicineproductlogoforjson MedicineProductLogoForJson { get; set; }
    public Medicineproductlogolink MedicineProductLogoLink { get; set; }
    public Varefaktakontrolleretlogoforjson VarefaktaKontrolleretLogoForJson { get; set; }
    public object VarefaktaKontrolleretLogoLink { get; set; }
}