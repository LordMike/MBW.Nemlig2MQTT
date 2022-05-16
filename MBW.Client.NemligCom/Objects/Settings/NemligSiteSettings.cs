namespace MBW.Client.NemligCom.Objects.Settings;

public class NemligSiteSettings
{
    public string BuildVersion { get; set; }
    public string SitecorePublishedStamp { get; set; }
    public string HomePageTitle { get; set; }
    public string ProductsImportedTimestamp { get; set; }
    public string CombinedProductsAndSitecoreTimestamp { get; set; }
    public string LoginPageUrl { get; set; }
    public string BasketPageUrl { get; set; }
    public string CreateUserPageUrl { get; set; }
    public string OrderConfirmationUrl { get; set; }
    public string ActivateUserPageUrl { get; set; }
    public string ResetPasswordPageUrl { get; set; }
    public string MyNemligPageUrl { get; set; }
    public string CustomerServicePageUrl { get; set; }
    public string NotFoundUrl { get; set; }
    public Timeslotbackgroundimage TimeslotBackgroundImage { get; set; }
    public string TimeslotDeliveryInformationLink { get; set; }
    public bool TimeslotUnattendedNewLabel { get; set; }
    public string UserId { get; set; }
    public int MemberType { get; set; }
    public string ZipCode { get; set; }
    public Salesforcesettings SalesforceSettings { get; set; }
    public int DeliveryZoneId { get; set; }
    public string TimeslotUtc { get; set; }
    public Sitelogossettings SiteLogosSettings { get; set; }
    public string MyNemligOrderHistoryPageUrl { get; set; }
    public string MyNemligPrintFriendlyPageUrl { get; set; }
    public string StaticResourcesPath { get; set; }
    public string NewsletterCompliancePageUrl { get; set; }
    public string ShoppingListOverViewPageUrl { get; set; }
    public string GoogleRecaptchaSiteKey { get; set; }
    public string MealboxesLandingPageUrl { get; set; }
    public string MealboxesPredefinedFlowPageUrl { get; set; }
    public string MealboxesBuildYourOwnFlowPageUrl { get; set; }
    public string GdprSettingsUrl { get; set; }
    public string NewsletterSettingsUrl { get; set; }
    public Gdprsettings GdprSettings { get; set; }
    public bool LoggedInThroughExternalResource { get; set; }
    public string PrivacyPolicyPageUrl { get; set; }
    public string CookiePolicyPageUrl { get; set; }
    public string TermsAndConditionsPageUrl { get; set; }
    public string MealboxesStartOrderingLinkUrl { get; set; }
    public int MaxAmountDates { get; set; }
    public string FavoritesPageUrl { get; set; }
    public string TilbudsPageUrl { get; set; }
}