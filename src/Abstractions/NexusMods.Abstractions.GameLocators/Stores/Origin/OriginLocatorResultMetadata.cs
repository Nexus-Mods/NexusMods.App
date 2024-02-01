using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators.Stores.Origin;

namespace NexusMods.Abstractions.Games.Stores.Origin;

/// <summary>
/// Metadata for games found that implement <see cref="IOriginGame"/>.
/// </summary>
[PublicAPI]
public record OriginLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required string Id { get; init; }
}
