namespace MBW.Client.NemligCom.Objects.Shared;

public interface INemligLine
{
    public int Quantity { get; }
    public string Description { get; }
    public string Name { get; }
    public int Id { get; }
    public float Price { get; }
}