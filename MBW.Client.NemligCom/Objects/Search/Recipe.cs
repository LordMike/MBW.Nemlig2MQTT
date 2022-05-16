namespace MBW.Client.NemligCom.Objects.Search;

public class Recipe
{
    public string TemplateName { get; set; }
    public string PrimaryImage { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Author { get; set; }
    public string TotalTime { get; set; }
    public string[] Tags { get; set; }
    public int NumberOfPersons { get; set; }
    public int[] AllowedForNumberOfPersons { get; set; }
    public Sortinglist1[] SortingList { get; set; }
}