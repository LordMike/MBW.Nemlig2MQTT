namespace MBW.Client.NemligCom.Objects.Settings;

public class Salesforcesettings
{
    public string Id { get; set; }
    public Questiontype[] QuestionTypes { get; set; }
    public string ContactEmail { get; set; }
    public Customerservicelink CustomerServiceLink { get; set; }
    public Faqlink Faqlink { get; set; }
    public string OrganizationID { get; set; }
    public string DeploymentID { get; set; }
    public string PopupButtonID { get; set; }
    public string FooterButtonID { get; set; }
    public string InitUrl { get; set; }
    public string MondayFrom { get; set; }
    public string MondayTo { get; set; }
    public string TuesdayFrom { get; set; }
    public string TuesdayTo { get; set; }
    public string WednesdayFrom { get; set; }
    public string WednesdayTo { get; set; }
    public string ThursdayFrom { get; set; }
    public string ThursdayTo { get; set; }
    public string FridayFrom { get; set; }
    public string FridayTo { get; set; }
    public string SaturdayFrom { get; set; }
    public string SaturdayTo { get; set; }
    public string SundayFrom { get; set; }
    public string SundayTo { get; set; }
}