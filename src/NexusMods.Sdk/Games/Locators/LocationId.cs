using NexusMods.Sdk.Hashes;
using TransparentValueObjects;

namespace NexusMods.Sdk.Games;

/// <summary>
/// The base folder for the GamePath.
/// </summary>
[ValueObject<ushort>]
public readonly partial struct LocationId
{
    private static readonly StringHashPool<ushort, FNV1a16Hasher> HashPool = new(name: nameof(LocationId));

    /// <summary>
    /// Converts the string to a LocationId.
    /// </summary>
    public static LocationId From(string value) => From(HashPool.GetOrAdd(value));

    /// <inheritdoc />
    public override string ToString() => HashPool[Value];

    /// <summary>
    /// Unknown game folder type, used for default values.
    /// </summary>
    public static readonly LocationId Unknown = From("Unknown");

    /// <summary>
    /// The path for the game installation.
    /// </summary>
    public static readonly LocationId Game = From("Game");

    /// <summary>
    /// Path used to store the save data of a game.
    /// </summary>
    public static readonly LocationId Saves = From("Saves");

    /// <summary>
    /// Path used to store player settings/preferences.
    /// </summary>
    public static readonly LocationId Preferences = From("Preferences");

    /// <summary>
    /// Path for game files located under LocalAppdata or equivalent.
    /// </summary>
    public static readonly LocationId AppData = From("AppData");

    /// <summary>
    /// Path for game files located under Appdata/Roaming or equivalent.
    /// </summary>
    public static readonly LocationId AppDataRoaming = From("AppDataRoaming");
}
