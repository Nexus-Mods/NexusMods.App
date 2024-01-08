using JetBrains.Annotations;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Metadata for games found that implement <see cref="IEADesktopGame"/>.
/// </summary>
[PublicAPI]
public record EADesktopLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required string SoftwareId { get; init; }
}
