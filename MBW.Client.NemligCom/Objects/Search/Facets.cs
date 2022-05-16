namespace MBW.Client.NemligCom.Objects.Search;

public class Facets
{
    public int NumFound { get; set; }
    public Sortinglist[] SortingList { get; set; }
    public Facetgroup[] FacetGroups { get; set; }
    public object[] FacetPivots { get; set; }
}