using NexusMods.DataModel.Abstractions.DTOs;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.Paths;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// A service for linking downloads with files in the archive manager
/// </summary>
public interface IDownloadRegistry
{
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
}
