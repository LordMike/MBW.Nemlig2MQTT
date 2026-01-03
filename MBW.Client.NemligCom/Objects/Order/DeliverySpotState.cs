namespace MBW.Client.NemligCom.Objects.Order;

public enum DeliverySpotState
{
    None = 0,
    Placed,
    Editing,
    Packing,
    ReadyForDelivery,
    OngoingDelivery,
    CompletedDelivery,
    Reorder
}
