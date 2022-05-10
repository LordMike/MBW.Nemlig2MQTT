namespace MBW.Nemlig2MQTT.HASS
{
    internal static class HassUniqueIdBuilder
    {
        public static string GetSystemDeviceId()
        {
            return "Nemlig2MQTT";
        }

        public static string GetBasketDeviceId()
        {
            return "basket";
        }

        public static string GetNextDeliveryDeviceId()
        {
            return "nxetdelivery";
        }
    }
}