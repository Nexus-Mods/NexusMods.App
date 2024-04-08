using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// Information about a file in an individual archive.
/// </summary>
[JsonName("NexusMods.DataModel.ArchiveContents.ArchivedFile")]
public record ArchivedFile : Entity
{
    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.ArchivedFiles;

    /// <summary>
    /// Name of the archive this file is contained in.
    /// </summary>
    public required RelativePath File { get; init; }

    /// <summary>
    /// The file entry data for the NX block offset
    /// </summary>
    public required byte[] FileEntryData { get; init; }
}
