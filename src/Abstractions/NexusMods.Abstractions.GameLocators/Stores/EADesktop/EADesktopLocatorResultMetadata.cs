using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators.Stores.EADesktop;

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

    /// <inheritdoc />
    public IEnumerable<LocatorId> ToLocatorIds() => [LocatorId.From(SoftwareId)];
}
