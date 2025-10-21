using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Identifier for a game on Nexus Mods.
/// </summary>
[ValueObject<uint>]
public readonly partial struct GameId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static GameId DefaultValue => From(0);
}

/// <summary>
/// Attribute for <see cref="GameId"/>.
/// </summary>
public class GameIdAttribute(string ns, string name) : ScalarAttribute<GameId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(GameId value) => value.Value;

    /// <inheritdoc />
    protected override GameId FromLowLevel(uint value, AttributeResolver resolver) => GameId.From(value);
}
