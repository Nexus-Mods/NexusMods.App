using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.DataModel.JsonConverters;

public class SizeConverter : JsonConverter<Size>
{
    public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Size.From(reader.GetUInt64());
    }

    public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((ulong)value);
    }
}
