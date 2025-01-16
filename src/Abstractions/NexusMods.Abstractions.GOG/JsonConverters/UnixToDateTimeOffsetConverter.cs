using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.GOG.JsonConverters;

/// <summary>
/// A converter for Unix timestamps (ulong seconds) to <see cref="DateTimeOffset"/>.
/// </summary>
internal class UnixToDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}
