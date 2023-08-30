using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.DTOs;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Registry for downloads, stores metadata and links to files in the archive manager
/// </summary>
public class DownloadRegistry : IDownloadRegistry
{
    private readonly ILogger<DownloadRegistry> _logger;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly IArchiveManager _archiveManager;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IDataStore _dataStore;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="extractor"></param>
    /// <param name="archiveManager"></param>
    public DownloadRegistry(ILogger<DownloadRegistry> logger, FileExtractor.FileExtractor extractor,
        IArchiveManager archiveManager, TemporaryFileManager temporaryFileManager, IDataStore store)
    {
        _logger = logger;
        _extractor = extractor;
        _archiveManager = archiveManager;
        _temporaryFileManager = temporaryFileManager;
        _dataStore = store;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default)
    {
        await using var tmpFolder = _temporaryFileManager.CreateFolder();

        await _extractor.ExtractAllAsync(path, tmpFolder.Path, token);
        return await RegisterFolder(tmpFolder.Path, metaData, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterFolder(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default)
    {
        List<ArchivedFileEntry> files = new();
        List<RelativePath> paths = new();

        _logger.LogInformation("Analyzing archive: {Name}", path);
        foreach (var file in path.EnumerateFiles())
        {
            // TODO: report this as progress
            var hash = await file.XxHash64Async(token: token);

            files.Add(new ArchivedFileEntry
            {
                Hash = hash,
                Size = file.FileInfo.Size,
                StreamFactory = new NativeFileStreamFactory(file)
            });
            paths.Add(file.RelativeTo(path));
        }

        _logger.LogInformation("Archiving {Count} files and {Size} of data", files.Count, files.Sum(f => f.Size));
        await _archiveManager.BackupFiles(files, token);

        Hash finalHash;
        Size finalSize;
        if (path.DirectoryExists())
        {
            finalHash = Hash.Zero;
            finalSize = Size.Zero;
        }
        else
        {
            finalHash = await path.XxHash64Async(token: token);
            finalSize = path.FileInfo.Size;
        }

        _logger.LogInformation("Calculating metadata");
        var analysis = new DownloadAnalysis()
        {
            DownloadId = DownloadId.New(),
            Hash = finalHash,
            Size = finalSize,
            Contents = paths.Zip(files).Select(pair =>
                new DownloadContentEntry
                {
                    Size = pair.Second.Size,
                    Hash = pair.Second.Hash,
                    Path = pair.First
                }).ToList(),
            MetaData = metaData
        };
        analysis.EnsurePersisted(_dataStore);
        return analysis.DownloadId;

    }

    /// <inheritdoc />
    public async ValueTask<DownloadAnalysis> Get(DownloadId id)
    {
        return _dataStore.Get<DownloadAnalysis>(IId.From(EntityCategory.DownloadMetadata, id.Value))!;
    }
}
