using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.GOG.JsonConverters;

public class GOGDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();

        var value = reader.GetString();
        if (DateTimeOffset.TryParse(value, out var result))
            return result;

        return DateTimeOffset.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss+zzz"));
    }
}
