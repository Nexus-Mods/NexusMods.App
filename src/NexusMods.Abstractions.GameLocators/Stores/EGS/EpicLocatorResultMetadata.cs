using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators.Stores.EGS;

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
    
    /// <summary>
    /// The hashes of the installed game manifests.
    /// </summary>
    public required IReadOnlyList<string> ManifestHashes { get; init; }

    /// <inheritdoc/>
    public ILinuxCompatibilityDataProvider? LinuxCompatibilityDataProvider { get; init; }

    /// <inheritdoc />
    public IEnumerable<LocatorId> ToLocatorIds() => ManifestHashes.Select(LocatorId.From);
}
