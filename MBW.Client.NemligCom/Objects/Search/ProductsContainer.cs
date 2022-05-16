namespace MBW.Client.NemligCom.Objects.Search;

public class ProductsContainer
{
    public Product[] Products { get; set; }
    public object ProductGroupId { get; set; }
    public int Start { get; set; }
    public int NumFound { get; set; }
}