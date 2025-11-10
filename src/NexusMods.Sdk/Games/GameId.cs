using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Sdk.Hashes;
using TransparentValueObjects;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Represents a globally unique identifier for a game type.
/// </summary>
[PublicAPI]
[ValueObject<ulong>]
public readonly partial struct GameId
{
    private static readonly StringHashPool<ulong, FNV1a64Hasher> HashPool = new(name: nameof(GameId));

    public static GameId From(string value) => From(HashPool.GetOrAdd(value));

    /// <inheritdoc/>
    public override string ToString() => HashPool[Value];
}

/// <summary>
/// Attribute for <see cref="GameId"/>.
/// </summary>
[PublicAPI]
public class GameIdAttribute(string ns, string name) : ScalarAttribute<GameId, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(GameId value) => value.Value;

    /// <inheritdoc />
    protected override GameId FromLowLevel(ulong value, AttributeResolver resolver) => GameId.From(value);
}
