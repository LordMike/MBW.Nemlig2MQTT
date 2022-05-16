namespace MBW.Client.NemligCom.Objects.Delivery;

public enum NemligDeliveryAvailability : byte
{
    Available = 0,
    PastDeadline = 1,
    SoldOut = 2
}