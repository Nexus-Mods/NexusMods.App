using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Sdk.Hashes;

internal class Sha1JsonConverter : JsonConverter<Sha1Value>
{
    public override Sha1Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (hex is null) throw new JsonException();

        return Sha1Value.FromHex(hex);
    }

    public override void Write(Utf8JsonWriter writer, Sha1Value value, JsonSerializerOptions options)
    {
        Span<char> hex = stackalloc char[Sha1Value.HexStringSize];

        var numWritten = value.ToHex(hex);
        Debug.Assert(numWritten == hex.Length, "entire span should be written to");

        writer.WriteStringValue(hex);
    }
}
