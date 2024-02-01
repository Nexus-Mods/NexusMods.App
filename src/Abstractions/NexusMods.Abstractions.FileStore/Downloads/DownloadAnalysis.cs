using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using ModFileTreeNode = NexusMods.Paths.Trees.KeyedBox<NexusMods.Paths.RelativePath, NexusMods.Abstractions.FileStore.Trees.ModFileTree>;

namespace NexusMods.Abstractions.Games.Downloads;

/// <summary>
/// Analysis of the contents of a download
/// </summary>
[JsonName("NexusMods.Abstractions.Games.Downloads.DownloadAnalysis")]
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
    /// Returns a file tree of the contents of the download
    /// </summary>
    public ModFileTreeNode GetFileTree() => TreeCreator.Create(Contents);

    /// <summary>
    /// Meta data for the download
    /// </summary>
    public AArchiveMetaData? MetaData { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.DownloadMetadata;

    /// <inheritdoc />
    protected override IId Persist(IDataStore store)
    {
        var id = IId.From(EntityCategory.DownloadMetadata, DownloadId.Value);
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

/// <summary>
///     Creates the tree! From the download content entries.
/// </summary>
public class TreeCreator
{
    /// <summary>
    ///     Creates the tree! From the download content entries.
    /// </summary>
    /// <param name="downloads">Downloads from the download registry.</param>
    /// <param name="fs">FileStore to read the files from.</param>
    public static ModFileTreeNode Create(IReadOnlyCollection<DownloadContentEntry> downloads, IFileStore? fs = null)
    {
        var entries = GC.AllocateUninitializedArray<ModFileTreeSource>(downloads.Count);
        var entryIndex = 0;
        foreach (var dl in downloads)
            entries[entryIndex++] = CreateSource(dl, fs != null ? new FileStoreStreamFactory(fs, dl.Hash) { Name = dl.Path, Size = dl.Size } : null);

        return ModFileTree.Create(entries);
    }

    /// <summary>
    ///     Creates a source file for the ModFileTree given a downloaded entry.
    /// </summary>
    /// <param name="entry">Entry for the individual file.</param>
    /// <param name="factory">Factory used to provide access to the file.</param>
    public static ModFileTreeSource CreateSource(DownloadContentEntry entry, IStreamFactory? factory)
    {
        return new ModFileTreeSource()
        {
            Hash = entry.Hash.Value,
            Size = entry.Size.Value,
            Path = entry.Path,
            Factory = factory
        };
    }
}
