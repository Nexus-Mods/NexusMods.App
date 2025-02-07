using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators.Stores.Xbox;

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

    /// <inheritdoc />
    public IEnumerable<string> ToLocatorIds() => [Id];
}
