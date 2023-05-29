using JetBrains.Annotations;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Metadata for games found that implement <see cref="IXboxGame"/>.
/// </summary>
[PublicAPI]
public record XboxLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required string Id { get; init; }
}
