using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MBW.Client.NemligCom.Objects.Order;

[JsonConverter(typeof(StringEnumConverter))]
public enum DeliverySpotState
{
    [EnumMember(Value = "None")]
    None = 0,
    [EnumMember(Value = "Placed")]
    Placed,
    [EnumMember(Value = "Editing")]
    Editing,
    [EnumMember(Value = "Packing")]
    Packing,
    [EnumMember(Value = "ReadyForDelivery")]
    ReadyForDelivery,
    [EnumMember(Value = "OngoingDelivery")]
    OngoingDelivery,
    [EnumMember(Value = "CompletedDelivery")]
    CompletedDelivery,
    [EnumMember(Value = "Reorder")]
    Reorder
}
