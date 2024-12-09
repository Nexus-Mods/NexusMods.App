using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Unique identifier for an individual game hosted on Nexus.
/// </summary>
[ValueObject<uint>] // Matches backend. Do not change.
[JsonConverter(typeof(GameIdJsonConverter))]
public readonly partial struct GameId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static GameId DefaultValue => From(default(uint));
}

internal class GameIdJsonConverter : JsonConverter<GameId>
{
    public override GameId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException();

        return GameId.From(reader.GetUInt32());
    }

    public override void Write(Utf8JsonWriter writer, GameId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

/// <summary>
/// Game ID attribute, for game identifiers from the GraphQL (V2) API.
/// </summary>
public class GameIdAttribute(string ns, string name) 
    : ScalarAttribute<GameId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(GameId value) => value.Value;

    /// <inheritdoc />
    protected override GameId FromLowLevel(uint value, AttributeResolver resolver) => GameId.From(value);
}
