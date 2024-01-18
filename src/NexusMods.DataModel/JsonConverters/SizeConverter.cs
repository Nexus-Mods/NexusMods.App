using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class SizeConverter : JsonConverter<Size>
{
    /// <inheritdoc />
    public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Size.From(reader.GetUInt64());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((ulong)value);
    }
}
