using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using DownloadId = NexusMods.Abstractions.FileStore.Downloads.DownloadId;

namespace NexusMods.Abstractions.FileStore;
using MetadataFn = Action<NexusMods.MnemonicDB.Abstractions.ITransaction, NexusMods.MnemonicDB.Abstractions.EntityId>;

/// <summary>
/// A service for linking downloads with files in the file store
/// </summary>
public interface IFileOriginRegistry
{
    /// <summary>
    /// Register a download with the registry, sourced from a stream, returns a download id that can be used to retrieve the download later.
    /// </summary>
    public ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, MetadataFn metaDataFn, CancellationToken token = default);

    /// <summary>
    /// Register a download with the registry, returns a download id that can be used to retrieve the download later.
    /// </summary>
    public ValueTask<DownloadId> RegisterDownload(AbsolutePath path, MetadataFn metaDataFn, CancellationToken token = default);

    /// <summary>
    /// Register a download with the registry, returns a download id that can be used to retrieve the download later,
    /// the name and source information are inferred from the path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<DownloadId> RegisterDownload(AbsolutePath path, CancellationToken token = default)
    {
        return RegisterDownload(path, (tx, id) =>
        {
            tx.Add(id, FilePathMetadata.OriginalName, path.FileName);
        }, token);
    }

    /// <summary>
    /// Indexes an already extracted download, returns a download id that can be used to retrieve the download later.
    /// </summary>
    public ValueTask<DownloadId> RegisterFolder(AbsolutePath path, MetadataFn metaDataFn, CancellationToken token = default);

    /// <summary>
    /// Get the analysis of a download
    /// </summary>
    public DownloadAnalysis.Model Get(DownloadId id);

    /// <summary>
    /// Get the analysis of all downloads
    /// </summary>
    public IEnumerable<DownloadAnalysis.Model> GetAll();

    /// <summary>
    /// Finds all downloads that have the given hash
    /// </summary>
    public IEnumerable<DownloadAnalysis.Model> GetBy(Hash hash);
}
