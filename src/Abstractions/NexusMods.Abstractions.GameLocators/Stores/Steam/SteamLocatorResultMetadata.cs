using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators.Stores.Steam;

/// <summary>
/// Metadata for games found that implement <see cref="ISteamGame"/>.
/// </summary>
[PublicAPI]
public record SteamLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required uint AppId { get; init; }

    /// <summary>
    /// Optional absolute path to the cloud saves directory for the game.
    /// </summary>
    public AbsolutePath? CloudSavesDirectory { get; init; }

    /// <summary>
    /// The `manifestIds` of the installed depots for a found game, according to Steam's associated `appmanifest` file
    /// </summary>
    public ulong[] ManifestIds { get; set; } = [];
    
    /// <inheritdoc />
    public IEnumerable<string> ToLocatorIds() => ManifestIds.Select(m => m.ToString());
}
