using JetBrains.Annotations;
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
