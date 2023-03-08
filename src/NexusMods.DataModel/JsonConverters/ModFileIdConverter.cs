using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class ModFileIdConverter : JsonConverter<ModFileId>
{
    /// <inheritdoc />
    public override ModFileId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var data = reader.GetBytesFromBase64();
        return ModFileId.From(new Guid(data));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ModFileId value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[16];
        value.Value.TryWriteBytes(span);
        writer.WriteBase64StringValue(span);
    }
}
