using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Sdk.Hashes;

internal class Md5JsonConverter : JsonConverter<Md5Value>
{
    public override Md5Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (hex is null) throw new JsonException();

        return Md5Value.FromHex(hex);
    }

    public override void Write(Utf8JsonWriter writer, Md5Value value, JsonSerializerOptions options)
    {
        Span<char> hex = stackalloc char[Md5Value.HexStringSize];

        var numWritten = value.ToHex(hex);
        Debug.Assert(numWritten == hex.Length, "entire span should be written to");

        writer.WriteStringValue(hex);
    }
}
