using JetBrains.Annotations;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Metadata for games found that implement <see cref="IEpicGame"/>.
/// </summary>
[PublicAPI]
public record EpicLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required string CatalogItemId { get; init; }
}
