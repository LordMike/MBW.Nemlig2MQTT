namespace MBW.Nemlig2MQTT.HASS;

internal static class HassUniqueIdBuilder
{
    public static string GetSystemDeviceId() => "Nemlig2MQTT";
    public static string GetBasketDeviceId() => "basket";
    public static string GetNextDeliveryDeviceId() => "nextdelivery";
    public static string GetOrderStatisticsDeviceId() => "orderstatistics";
}