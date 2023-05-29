using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("ArchivedFiles")]
public record ArchivedFiles : Entity
{
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
