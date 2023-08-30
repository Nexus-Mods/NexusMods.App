using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Abstractions.DTOs;


/// <summary>
/// Analysis of the contents of a download
/// </summary>
public record DownloadAnalysis : Entity
{
    /// <summary>
    /// The id of the download
    /// </summary>
    public required DownloadId DownloadId { get; init; }

    /// <summary>
    /// The hash of the download
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Size of the download
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// The files contained in the download
    /// </summary>
    public required IReadOnlyCollection<DownloadContentEntry> Contents { get; init; }

    /// <summary>
    /// Meta data for the download
    /// </summary>
    public AArchiveMetaData? MetaData { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.DownloadMetadata;

    /// <inheritdoc />
    protected override IId Persist(IDataStore store)
    {
        var id = IId.From(EntityCategory.DownloadMetadata, ArchiveId.Value);
        store.Put(id, this);
        return id;
    }
}


/// <summary>
/// A single entry in the download analysis, this is a file that is contained in the download
/// </summary>
public record DownloadContentEntry
{
    /// <summary>
    /// Hash of the file
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Size of the file
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// Path of the file
    /// </summary>
    public required RelativePath Path { get; init; }
}
