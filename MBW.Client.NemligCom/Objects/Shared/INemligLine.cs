namespace MBW.Client.NemligCom.Objects.Basket;

public interface INemligLine
{
    public int Quantity { get; }
    public string Description { get; }
    public string Name { get; }
    public int Id { get; }
    public float Price { get; }
}