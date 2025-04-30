using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Serialization.Json;

/// <inheritdoc/>
[UsedImplicitly]
public class EntityIdConverter : JsonConverter<EntityId>
{
    /// <inheritdoc/>
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetUInt64();
        return EntityId.From(value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
