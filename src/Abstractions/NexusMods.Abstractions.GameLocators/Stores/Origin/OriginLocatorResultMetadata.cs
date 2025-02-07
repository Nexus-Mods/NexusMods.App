using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators.Stores.Origin;

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

    /// <inheritdoc />
    public IEnumerable<string> ToLocatorIds() => [Id];
}
