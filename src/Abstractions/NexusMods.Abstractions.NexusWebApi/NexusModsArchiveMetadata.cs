using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Abstractions.FileStore.ArchiveMetadata;

/// <summary>
/// Archive metadata for a download that was installed from a NexusMods mod.
/// </summary>
[JsonName("NexusMods.Abstractions.Games.ArchiveMetadata.NexusModsArchiveMetadata")]
public record NexusModsArchiveMetadata : AArchiveMetaData
{
    /// <summary>
    /// The NexusMods API game ID.
    /// </summary>
    public required GameDomain GameDomain { get; init; }

    /// <summary>
    /// Mod ID corresponding to the Nexus API.
    /// </summary>
    public required ModId ModId { get; init; }

    /// <summary>
    /// File ID corresponding to the Nexus API.
    /// </summary>
    public required FileId FileId { get; init; }
}
