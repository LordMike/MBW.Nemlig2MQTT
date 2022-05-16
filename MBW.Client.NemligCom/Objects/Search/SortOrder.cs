using System.Runtime.Serialization;

namespace MBW.Client.NemligCom.Objects.Search;

public enum SortOrder
{
    None,

    [EnumMember(Value = "recommended")]
    Recommended,

    [EnumMember(Value = "navn")]
    Alphabetical,

    [EnumMember(Value = "brand")]
    Brand,

    [EnumMember(Value = "price")]
    CheapestFirst,

    [EnumMember(Value = "dyrest")]
    ExpensiveFirst,

    [EnumMember(Value = "type")]
    Type
}