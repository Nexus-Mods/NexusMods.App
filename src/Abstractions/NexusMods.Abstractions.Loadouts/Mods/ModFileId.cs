using System.Text.Json;
using System.Text.Json.Serialization;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Loadouts.Mods;

/// <summary>
/// A unique identifier for a file which belongs to a mod in the data store/database.
/// These IDs are assigned to files as they are found, i.e. during archive scan/file
/// discovery step.
/// </summary>
[ValueObject<Guid>]
public readonly partial struct ModFileId { }

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
