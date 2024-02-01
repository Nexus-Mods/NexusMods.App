using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Games.Stores.Steam;

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
}
