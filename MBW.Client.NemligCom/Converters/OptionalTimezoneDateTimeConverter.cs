using System;
using Newtonsoft.Json;

namespace MBW.Client.NemligCom.Converters;

internal class OptionalTimezoneDateTimeConverter : JsonConverter<DateTimeOffset?>
{
    private readonly TimeZoneInfo _tz;

    public OptionalTimezoneDateTimeConverter(string timezone)
    {
        _tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
    }

    public override void WriteJson(JsonWriter writer, DateTimeOffset? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override DateTimeOffset? ReadJson(JsonReader reader, Type objectType, DateTimeOffset? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var dt = reader.Value as DateTime?;
        if (!dt.HasValue)
            return null;

        if (dt.Value.Kind == DateTimeKind.Unspecified)
            return new DateTimeOffset(dt.Value, _tz.GetUtcOffset(dt.Value));

        return dt.Value;
    }
}