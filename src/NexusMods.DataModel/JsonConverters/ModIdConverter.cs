using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class ModIdConverter : JsonConverter<ModId>
{
    /// <inheritdoc />
    public override ModId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var data = reader.GetBytesFromBase64();
        return ModId.From(new Guid(data));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ModId value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[16];
        value.Value.TryWriteBytes(span);
        writer.WriteBase64StringValue(span);
    }
}
