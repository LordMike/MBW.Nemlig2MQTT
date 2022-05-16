namespace MBW.Client.NemligCom.Objects.Search;

public class NemligSearchResponse
{
    public ProductsContainer Products { get; set; }
    public Facets Facets { get; set; }
    public object[] Suggestions { get; set; }
    public Recipe[] Recipes { get; set; }
    public Ad[] Ads { get; set; }
    public int RecipesNumFound { get; set; }
    public int ProductsNumFound { get; set; }
    public string SearchQuery { get; set; }
    public int AdsNumFound { get; set; }
}