using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators.Stores.GOG;

/// <summary>
/// Metadata for games found that implement <see cref="IGogGame"/>.
/// </summary>
[PublicAPI]
public record GOGLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required long Id { get; init; }
    
    /// <summary>
    /// The build ID of the found game.
    /// </summary>
    public required string BuildId { get; init; }
    
    /// <inheritdoc />
    public IEnumerable<LocatorId> ToLocatorIds() => [LocatorId.From(BuildId)];
}

[PublicAPI]
public record HeroicGOGLocatorResultMetadata : GOGLocatorResultMetadata;
