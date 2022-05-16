namespace MBW.Client.NemligCom.Objects.Menu;

public class NemligMenuNode
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Text { get; set; }
    public string AppMenuImageUrl { get; set; }
    public NemligMenuNode[] Children { get; set; }
    public bool IsMegaMenuItem { get; set; }
    public bool StopInApp { get; set; }
}