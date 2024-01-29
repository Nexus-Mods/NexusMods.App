using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Paths;

namespace NexusMods.Abstractions.FileStore.ArchiveMetadata;

/// <summary>
/// Archive metadata for a download that was installed from a file path.
/// </summary>
[JsonName("NexusMods.Abstractions.Games.ArchiveMetadata.FilePathMetadata")]
public record FilePathMetadata : AArchiveMetaData
{
    /// <summary>
    /// The filename portion of the path.
    /// </summary>
    public RelativePath OriginalName { get; init; } = RelativePath.Empty;
}
