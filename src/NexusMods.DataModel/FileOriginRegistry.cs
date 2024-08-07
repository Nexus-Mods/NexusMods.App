using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library;
using NexusMods.App.BuildInfo;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

using MetadataFn = System.Action<NexusMods.MnemonicDB.Abstractions.ITransaction, NexusMods.MnemonicDB.Abstractions.EntityId>;

namespace NexusMods.DataModel;

/// <summary>
/// Registry for downloads, stores metadata and links to files in the file store
/// </summary>
[Obsolete("To be replaced with LibraryService")]
public class FileOriginRegistry : IFileOriginRegistry
{
    private readonly ILogger<FileOriginRegistry> _logger;
    private readonly ILibraryService _libraryService;
    private readonly IFileExtractor _extractor;
    private readonly IFileStore _fileStore;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IConnection _conn;

    /// <summary>
    /// Constructor.
    /// </summary>
    public FileOriginRegistry(
        ILogger<FileOriginRegistry> logger,
        ILibraryService library,
        IFileExtractor extractor,
        IFileStore fileStore,
        TemporaryFileManager temporaryFileManager,
        IConnection conn)
    {
        _logger = logger;
        _libraryService = library;
        _extractor = extractor;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
        _conn = conn;
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(IStreamFactory factory, MetadataFn metaData, string modName, CancellationToken token = default)
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

        return await RegisterFolderInternal(tmpFolder.Path, AppendNestedArchiveMetadata, null, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, modName, token);

        void AppendNestedArchiveMetadata(ITransaction tx, EntityId id)
        {
            metaData?.Invoke(tx, id);
            tx.Add(id, StreamBasedFileOriginMetadata.StreamBasedOrigin, new Null());
        }
    }

    private async Task ShadowTrafficTestLibraryService(AbsolutePath path, CancellationToken cancellationToken)
    {
        // TODO: https://github.com/Nexus-Mods/NexusMods.App/issues/1763
        if (!CompileConstants.IsDebug) return;
        try
        {
            await using var job = _libraryService.AddLocalFile(path);
            await job.StartAsync(cancellationToken: cancellationToken);
            var result = await job.WaitToFinishAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("AddLocalFile result: `{Result}`", result.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception adding local file to library");
        }
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(AbsolutePath path, MetadataFn metaDataFn, string modName, CancellationToken token = default)
    {
        await ShadowTrafficTestLibraryService(path, token);

        var db = _conn.Db;
        var archiveSize = (ulong) path.FileInfo.Size;
        var archiveHash = await path.XxHash64Async(token: token);

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(db, archiveHash, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, metaDataFn, null, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, modName, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterDownload(AbsolutePath path, EntityId id, string modName, CancellationToken token = default)
    {
        await ShadowTrafficTestLibraryService(path, token);

        var db = _conn.Db;
        var archiveSize = (ulong) path.FileInfo.Size;
        var archiveHash = await path.XxHash64Async(token: token);

        // Note: Folders have a hash of 0, so in unlikely event an archive hashes to 0, we can't dedupe by archive.
        if (archiveHash != 0 && TryGetDownloadIdForHash(db, archiveHash, out var downloadId))
            return downloadId.Value;

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tmpFolder.Path, token);
        return await RegisterFolderInternal(tmpFolder.Path, null, id, _fileStore.GetFileHashes(), archiveHash.Value, archiveSize, modName, token);
    }

    /// <inheritdoc />
    public async ValueTask<DownloadId> RegisterFolder(AbsolutePath path, Action<ITransaction, EntityId> metaDataFn, string modName,
        CancellationToken token = default)
    {
        return await RegisterFolderInternal(path, metaDataFn, null, _fileStore.GetFileHashes(), 0, 0, modName, token);
    }

    /// <inheritdoc />
    public DownloadAnalysis.ReadOnly Get(DownloadId id)
    {
        return DownloadAnalysis.Load(_conn.Db, id.Value);
    }

    /// <inheritdoc />
    public IEnumerable<DownloadAnalysis.ReadOnly> GetAll()
    {
        return DownloadAnalysis.All(_conn.Db);
    }

    /// <inheritdoc />
    public IEnumerable<DownloadAnalysis.ReadOnly> GetBy(Hash hash)
    {
        return DownloadAnalysis.FindByHash(_conn.Db, hash);
    }

    private async ValueTask<DownloadId> RegisterFolderInternal(AbsolutePath originalPath, 
        Action<ITransaction, EntityId>? metaDataFn, 
        EntityId? existingId,
        HashSet<ulong> knownHashes, 
        ulong archiveHash, 
        ulong archiveSize, 
        string suggestedName,
        CancellationToken token = default)
    {
        List<ArchivedFileEntry> filesToBackup = [];
        List<ArchivedFileEntry> files = [];
        List<RelativePath> paths = [];

        _logger.LogInformation("Analyzing archive: {Name}", originalPath);
        
        // Note: We exploit Async I/O here. Modern storage can munch files in parallel,
        // so doing this on one thread would be a waste.

        var allFiles = originalPath.EnumerateFiles().ToArray(); // enables better work stealing.
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
            var relativePath = file.RelativeTo(originalPath);
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
        using var tx = _conn.BeginTransaction();


        existingId ??= tx.TempId();
        
        var analysis = new DownloadAnalysis.New(tx, existingId.Value)
        {
            Hash = Hash.From(archiveHash),
            Size = Size.From(archiveSize),
            NumberOfEntries = (ulong) files.Count,
            SuggestedName = suggestedName,
        };

        metaDataFn?.Invoke(tx, analysis.Id);

        foreach (var (path, file) in paths.Zip(files))
        {
            _ = new DownloadContentEntry.New(tx)
            {
                Size = file.Size,
                Hash = file.Hash,
                Path = path,
                DownloadAnalysisId = analysis.Id,
            };
        }

        var id = (await tx.Commit())[analysis.Id];
        
        return DownloadId.From(id);
    }

    /// <summary>
    ///     Gets a <see cref="DownloadId"/> with a given hash.
    /// </summary>
    private bool TryGetDownloadIdForHash(IDb? db, Hash hash, [NotNullWhen(true)] out DownloadId? analysis)
    {
        db ??= _conn.Db;

        foreach (var found in DownloadAnalysis.FindByHash(db, hash))
        {
            analysis = DownloadId.From(found);
            return true;
        }
        
        analysis = null;
        return false;
    }
}
