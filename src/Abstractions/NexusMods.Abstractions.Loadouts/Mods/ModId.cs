using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Loadouts.Mods;

/// <summary>
/// A unique identifier for the mod for use within the data store/database.
/// These IDs are assigned to mods upon installation (i.e. when a mod is
/// added to a loadout), or when a tool generates some files after running.
/// </summary>
[ValueObject<Guid>]
[JsonConverter(typeof(ModIdConverter))]
public readonly partial struct ModId { }

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
