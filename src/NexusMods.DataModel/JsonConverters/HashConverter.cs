using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.JsonConverters;

public class HashConverter : JsonConverter<Hash>
{
    public override Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetUInt64();
    }

    public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((ulong)value);
    }
}