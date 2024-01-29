using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;

namespace NexusMods.Abstractions.Games.Stores.Xbox;

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
