using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Shared;

namespace MBW.Client.NemligCom.Objects.Order;

public class OrderHistoryLine : INemligLine
{
    public string GroupName { get; set; }
    public int ProductNumber { get; set; }
    public string ProductName { get; set; }
    //public object RecipeId { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; }
    public float AverageItemPrice { get; set; }
    public float Amount { get; set; }
    public bool IsDepositLine { get; set; }
    public bool IsProductLine { get; set; }
    public bool IsRecipeLine { get; set; }
    public string RecipeUrl { get; set; }
    public bool IsMealBoxLine { get; set; }
    public bool IsMealBoxRecipeLine { get; set; }
    public string MealBoxRecipePdfUrl { get; set; }
    public string MainGroupName { get; set; }
    public string CampaignName { get; set; }
    public bool IsMixLine { get; set; }
    public float DiscountAmount { get; set; }
    public int SoldOut { get; set; }
    public float OriginalQuantity { get; set; }
    public float OriginalItemPrice { get; set; }
    //public object OriginalProductNumber { get; set; }
    //public object OriginalProductName { get; set; }

    public string Name => ProductName;
    public int Id => ProductNumber;

    public float Price => AverageItemPrice;
}