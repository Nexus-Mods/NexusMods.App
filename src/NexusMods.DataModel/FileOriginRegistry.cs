using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Registry for downloads, stores metadata and links to files in the file store
/// </summary>
public class FileOriginRegistry : IFileOriginRegistry
{
    private readonly ILogger<FileOriginRegistry> _logger;
    private readonly IFileExtractor _extractor;
    private readonly IFileStore _fileStore;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IDataStore _dataStore;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="extractor"></param>
    /// <param name="fileStore"></param>
    /// <param name="temporaryFileManager"></param>
    /// <param name="store"></param>
    public FileOriginRegistry(ILogger<FileOriginRegistry> logger, IFileExtractor extractor,
        IFileStore fileStore, TemporaryFileManager temporaryFileManager, IDataStore store)
    {
        _logger = logger;
        _extractor = extractor;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
        _dataStore = store;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, AArchiveMetaData metaData, CancellationToken token = default)
    {
        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(factory, tmpFolder.Path, token);
        return await RegisterFolder(tmpFolder.Path, metaData, token);
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
        List<ArchivedFileEntry> filesToBackup = new();
        List<ArchivedFileEntry> files = new();
        List<RelativePath> paths = new();

        // Build a Hash Table of all currently known files. We do this to deduplicate files between downloads.
        // TODO: This may need some benchmarking, it's unknown how well this scales across huge loadouts.
        var existingDownloadIds = _dataStore.AllIds(EntityCategory.DownloadMetadata);
        var knownHashes = new HashSet<ulong>();
        foreach (var id in existingDownloadIds)
        {
            var ent = _dataStore.Get<DownloadAnalysis>(id);

            // shouldn't be null but technically possible in case of concurrent modifications
            if (ent == null)
                continue;

            foreach (var content in ent.Contents)
                knownHashes.Add(content.Hash.Value);
        }

        _logger.LogInformation("Analyzing archive: {Name}", path);
        foreach (var file in path.EnumerateFiles())
        {
            // TODO: report this as progress
            var hash = await file.XxHash64Async(token: token);
            var archivedEntry = new ArchivedFileEntry
            {
                Hash = hash,
                Size = file.FileInfo.Size,
                StreamFactory = new NativeFileStreamFactory(file)
            };

            // If the hash isn't known, we should back it up.
            paths.Add(file.RelativeTo(path));
            files.Add(archivedEntry);
            if (!knownHashes.Contains(hash.Value))
                filesToBackup.Add(archivedEntry);
        }

        // We don't want to risk creating an empty archive depending on underlying implementation if
        // it's all duplicates.
        if (filesToBackup.Count > 0)
        {
            _logger.LogInformation("Archiving {Count} files and {Size} of data", filesToBackup.Count, filesToBackup.Sum(f => f.Size));
            await _fileStore.BackupFiles(filesToBackup, token);
        }
        else
        {
            _logger.LogInformation("All files are duplicates, there is nothing to backup");
        }

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
            DownloadId = DownloadId.NewId(),
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

        _dataStore.AllIds(EntityCategory.DownloadMetadata);
        analysis.EnsurePersisted(_dataStore);
        return analysis.DownloadId;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadAnalysis> Get(DownloadId id)
    {
        return _dataStore.Get<DownloadAnalysis>(IId.From(EntityCategory.DownloadMetadata, id.Value))!;
    }

    /// <inheritdoc />
    public IEnumerable<DownloadAnalysis> GetAll()
    {
        return _dataStore.GetByPrefix<DownloadAnalysis>(new Id64(EntityCategory.DownloadMetadata, 0));
    }

    /// <inheritdoc />
    public IEnumerable<DownloadId> GetByHash(Hash hash)
    {
        return GetAll()
            .Where(d => d.Hash == hash)
            .Select(d => d.DownloadId);
    }
}
