using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Games.Loadouts;

/// <summary>
/// A service for linking downloads with files in the file store
/// </summary>
public interface IFileOriginRegistry
{
    /// <summary>
    /// Register a download with the registry, sourced from a stream, returns a download id that can be used to retrieve the download later.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="metaData"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, AArchiveMetaData metaData, CancellationToken token = default);

    /// <summary>
    /// Register a download with the registry, returns a download id that can be used to retrieve the download later.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="metaData"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<DownloadId> RegisterDownload(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default);

    /// <summary>
    /// Indexes an already extracted download, returns a download id that can be used to retrieve the download later.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="metaData"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<DownloadId> RegisterFolder(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default);

    /// <summary>
    /// Get the analysis of a download
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ValueTask<DownloadAnalysis> Get(DownloadId id);

    /// <summary>
    /// Get the analysis of all downloads
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DownloadAnalysis> GetAll();

    /// <summary>
    /// Finds all downloads that have the given hash
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public IEnumerable<DownloadId> GetByHash(Hash hash);
}
