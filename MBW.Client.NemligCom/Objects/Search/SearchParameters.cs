namespace MBW.Client.NemligCom.Objects.Search;

public class SearchParameters
{
    public string Query { get; set; }

    public SearchFilters Filters { get; set; }

    public int? Skip { get; set; }

    public int? Take { get; set; }

    public SortOrder SortOrder { get; set; }
}