using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Games.FileHashes.HashValues;

internal class HashJsonConverter : JsonConverter<Hash>
{
    public override Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Hash.FromULong(reader.GetUInt64());
    }

    public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
