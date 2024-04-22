using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

using MetadataFn = System.Action<NexusMods.MnemonicDB.Abstractions.ITransaction, NexusMods.MnemonicDB.Abstractions.EntityId>;

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
    private readonly IConnection _conn;
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
        IFileStore fileStore, TemporaryFileManager temporaryFileManager, IConnection conn, IFileHashCache fileHashCache)
    {
        _logger = logger;
        _extractor = extractor;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
        _conn = conn;
        _fileHashCache = fileHashCache;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, MetadataFn metaData, CancellationToken token = default)
    {
        var db = _conn.Db;
        // WARNING !! Cannot access hash cache.
        var archiveSize = (ulong) factory.Size;
        var archiveHash = await (await factory.GetStreamAsync()).XxHash64Async(token: token);

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(db, archiveHash, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(factory, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, metaData, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(AbsolutePath path, MetadataFn metaDataFn, CancellationToken token = default)
    {
        var db = _conn.Db;
        var archiveSize = (ulong) path.FileInfo.Size;
        var archiveHash = (await _fileHashCache.IndexFileAsync(path, token)).Hash;

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(db, archiveHash, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, metaDataFn, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterFolder(AbsolutePath path, Action<ITransaction, EntityId> metaDataFn,
        CancellationToken token = default)
    {
        return await RegisterFolderInternal(path, metaDataFn, _fileStore.GetFileHashes(), 0, 0, token);
    }

    /// <inheritdoc />
    public DownloadAnalysis.Model Get(DownloadId id)
    {
        var db = _conn.Db;
        return db.Get<DownloadAnalysis.Model>(id.Value);
    }

    /// <inheritdoc />
    public IEnumerable<DownloadAnalysis.Model> GetAll()
    {
        var db = _conn.Db;
        return db.Find(DownloadAnalysis.NumberOfEntries)
                 .Select(id => db.Get<DownloadAnalysis.Model>(id));
    }

    /// <inheritdoc />
    public IEnumerable<DownloadAnalysis.Model> GetBy(Hash hash)
    {
        var db = _conn.Db;
        return db.FindIndexed(hash, DownloadAnalysis.Hash)
                 .Select(id => db.Get<DownloadAnalysis.Model>(id));
    }

    private async ValueTask<DownloadId> RegisterFolderInternal(AbsolutePath originalPath, Action<ITransaction, EntityId> metaDataFn, 
        HashSet<ulong> knownHashes, ulong archiveHash, ulong archiveSize, CancellationToken token = default)
    {
        List<ArchivedFileEntry> filesToBackup = [];
        List<ArchivedFileEntry> files = [];
        List<RelativePath> paths = [];

        _logger.LogInformation("Analyzing archive: {Name}", originalPath);
        foreach (var file in originalPath.EnumerateFiles())
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
            paths.Add(file.RelativeTo(originalPath));
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

        _logger.LogInformation("Calculating metadata");
        using var tx = _conn.BeginTransaction();
        var analysis = new DownloadAnalysis.Model(tx)
        {
            Hash = Hash.From(archiveHash),
            Size = Size.From(archiveSize),
            Count = (ulong) files.Count,
        };
        metaDataFn(tx, analysis.Id);
        
        foreach (var (path, file) in paths.Zip(files))
        {
            _ = new DownloadContentEntry.Model(tx)
            {
                Size = file.Size,
                Hash = file.Hash,
                Path = path,
                DownloadAnalysisId = DownloadId.From(analysis.Id),
            };
        }

        var id = (await tx.Commit())[analysis.Id];
        
        return DownloadId.From(id);
    }

    /// <summary>
    ///     Gets a <see cref="DownloadId"/> with a given hash.
    /// </summary>
    private bool TryGetDownloadIdForHash(IDb? db, Hash expectedHash, [NotNullWhen(true)] out DownloadId? analysis)
    {
        db ??= _conn.Db;

        foreach (var found in db.FindIndexed(expectedHash, DownloadAnalysis.Hash))
        {
            analysis = DownloadId.From(found);
            return true;
        }
        
        analysis = null;
        return false;
    }
}
