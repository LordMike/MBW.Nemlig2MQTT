namespace MBW.Client.NemligCom.Objects.Search;

public class Product1
{
    public float Score { get; set; }
    public string TemplateName { get; set; }
    public string PrimaryImage { get; set; }
    public Availability1 Availability { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Brand { get; set; }
    public string SubCategory { get; set; }
    public string Url { get; set; }
    public string UnitPrice { get; set; }
    public float UnitPriceCalc { get; set; }
    public string UnitPriceLabel { get; set; }
    public bool DiscountItem { get; set; }
    public string Description { get; set; }
    public int SaleBeforeLastSalesDate { get; set; }
    public float Price { get; set; }
    public object Campaign { get; set; }
    public string[] Labels { get; set; }
    public string[] SearchDescription { get; set; }
    public string ProductSubGroupNumber { get; set; }
    public object ProductSubGroupName { get; set; }
    public string ProductCategoryGroupNumber { get; set; }
    public string ProductCategoryGroupName { get; set; }
    public string ProductMainGroupNumber { get; set; }
    public string ProductMainGroupName { get; set; }
}