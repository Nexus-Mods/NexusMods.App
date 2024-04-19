using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.ArchiveContents;
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
    private readonly IFileHashCache _fileHashCache;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="extractor"></param>
    /// <param name="fileStore"></param>
    /// <param name="temporaryFileManager"></param>
    /// <param name="store"></param>
    public FileOriginRegistry(ILogger<FileOriginRegistry> logger, IFileExtractor extractor,
        IFileStore fileStore, TemporaryFileManager temporaryFileManager, IDataStore store, IFileHashCache fileHashCache)
    {
        _logger = logger;
        _extractor = extractor;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
        _dataStore = store;
        _fileHashCache = fileHashCache;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, AArchiveMetaData metaData, CancellationToken token = default)
    {
        // WARNING !! Cannot access hash cache.
        var archiveSize = (ulong) factory.Size;
        var archiveHash = await (await factory.GetStreamAsync()).XxHash64Async(token: token);

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(archiveHash.Value, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(factory, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, metaData, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default)
    {
        var archiveSize = (ulong) path.FileInfo.Size;
        var archiveHash = (await _fileHashCache.IndexFileAsync(path, token)).Hash;

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(archiveHash.Value, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, metaData, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterFolder(AbsolutePath path, AArchiveMetaData metaData, CancellationToken token = default)
    {
        return await RegisterFolderInternal(path, metaData, _fileStore.GetFileHashes(), 0, 0, token);
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

    private async ValueTask<DownloadId> RegisterFolderInternal(AbsolutePath path, AArchiveMetaData metaData, HashSet<ulong> knownHashes, ulong archiveHash, ulong archiveSize, CancellationToken token = default)
    {
        List<ArchivedFileEntry> filesToBackup = new();
        List<ArchivedFileEntry> files = new();
        List<RelativePath> paths = new();

        _logger.LogInformation("Analyzing archive: {Name}", path);
        
        // Note: We exploit Async I/O here. Modern storage can munch files in parallel,
        // so doing this on one thread would be a waste.

        var allFiles = path.EnumerateFiles().ToArray(); // enables better work stealing.
        Parallel.ForEach(allFiles, file =>
        {
            // TODO: report this as progress
            var hash = file.XxHash64MemoryMapped();
            var archivedEntry = new ArchivedFileEntry
            {
                Hash = hash,
                Size = file.FileInfo.Size,
                StreamFactory = new NativeFileStreamFactory(file),
            };

            // If the hash isn't known, we should back it up.
            var relativePath = file.RelativeTo(path);
            lock (paths)
            {
                paths.Add(relativePath);
                files.Add(archivedEntry);
                if (!knownHashes.Contains(hash.Value))
                    filesToBackup.Add(archivedEntry);
            }
        }
        );

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

        _logger.LogInformation("Calculating metadata");
        var analysis = new DownloadAnalysis()
        {
            DownloadId = DownloadId.NewId(),
            Hash = Hash.From(archiveHash),
            Size = Size.From(archiveSize),
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

    /// <summary>
    ///     Gets a <see cref="DownloadId"/> with a given hash.
    /// </summary>
    private bool TryGetDownloadIdForHash(ulong expectedHash, [NotNullWhen(true)] out DownloadId? analysis)
    {
        foreach (var ent in _dataStore.GetAll<DownloadAnalysis>(EntityCategory.DownloadMetadata)!)
        {
            if (ent.Hash != expectedHash)
                continue;

            analysis = ent.DownloadId;
            return true;
        }

        analysis = default;
        return false;
    }
}
