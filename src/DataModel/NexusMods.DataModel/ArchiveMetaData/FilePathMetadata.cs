using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveMetaData;

/// <summary>
/// Archive metadata for a download that was installed from a file path.
/// </summary>
[JsonName("NexusMods.DataModel.ArchiveMetaData.FilePathMetadata")]
public record FilePathMetadata : AArchiveMetaData
{
    /// <summary>
    /// The filename portion of the path.
    /// </summary>
    public RelativePath OriginalName { get; init; } = RelativePath.Empty;
}
