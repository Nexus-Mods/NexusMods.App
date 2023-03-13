using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class DateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.FromFileTimeUtc(reader.GetInt64());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToFileTimeUtc());
    }
}
