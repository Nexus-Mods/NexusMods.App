using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Collections.Json;

[JsonConverter(typeof(PatchHashJsonConverter))]
[ValueObject<uint>]
public readonly partial struct PatchHash
{
    
}

public class PatchHashJsonConverter : JsonConverter<PatchHash>
{
    public override PatchHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PatchHash.From(uint.Parse(reader.GetString()!, NumberStyles.HexNumber));
    }

    public override void Write(Utf8JsonWriter writer, PatchHash value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString("X"));
    }
}
