using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public sealed class LibraryService : ILibraryService, IDisposable
{
    private readonly ILogger _logger;
    private readonly CompositeDisposable _compositeDisposable = new();

    private readonly IConnection _connection;
    private readonly IFileExtractor _fileExtractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    private readonly SourceCache<IDownloadActivity, PersistedDownloadStateId> _downloadActivitySourceCache = new(x => x.PersistedStateId);
    private readonly ReadOnlyObservableCollection<IDownloadActivity> _downloadActivities;
    public ReadOnlyObservableCollection<IDownloadActivity> DownloadActivities => _downloadActivities;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(
        ILogger<LibraryService> logger,
        IConnection connection,
        IFileExtractor fileExtractor,
        TemporaryFileManager temporaryFileManager)
    {
        _logger = logger;
        _connection = connection;
        _fileExtractor = fileExtractor;
        _temporaryFileManager = temporaryFileManager;

        _downloadActivitySourceCache
            .Connect()
            .Bind(out _downloadActivities)
            .Subscribe()
            .DisposeWith(_compositeDisposable);

        // handle completed downloads
        _downloadActivitySourceCache
            .Connect()
            .WhenPropertyChanged(downloadActivity => downloadActivity.Status)
            .Where(propertyValue => propertyValue.Value == PersistedDownloadStatus.Completed)
            .Select(propertyValue => propertyValue.Sender)
            .Select(downloadActivity => Observable.FromAsync(cancellationToken => AddCompletedDownloadAsync(downloadActivity, cancellationToken)))
            .SelectMany(sequences => sequences)
            .Subscribe()
            .DisposeWith(_compositeDisposable);
    }

    /// <inheritdoc/>
    public void EnqueueDownload(IDownloadActivity downloadActivity, bool addPaused = false)
    {
        _logger.LogInformation("Adding download `{Title}` to the library", downloadActivity.Title);

        _downloadActivitySourceCache.AddOrUpdate(downloadActivity);

        if (addPaused) return;
        downloadActivity.Downloader.Start(downloadActivity);
    }

    private async Task<LibraryFile.ReadOnly> AddCompletedDownloadAsync(
        IDownloadActivity downloadActivity,
        CancellationToken cancellationToken)
    {
        // TODO: figure out when to remove activity from the cache
        // _downloadActivitySourceCache.Remove(downloadActivity);

        using var tx = _connection.BeginTransaction();
        var entityId = tx.TempId();

        // TODO: create download specific library item
        var libraryFile = await AddLibraryFileAsync(tx, entityId, downloadActivity.DownloadPath, cancellationToken: cancellationToken);

        var result = await tx.Commit();
        return result.Remap(libraryFile);
    }

    /// <inheritdoc/>
    public async Task<Optional<LocalFile.ReadOnly>> AddLocalFileAsync(
        AbsolutePath absolutePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding local file at `{Path}` to the library", absolutePath);

        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                _logger.LogError("File at `{Path}` can't be added to the library because it's a directory", absolutePath);
            }

            _logger.LogError("File at `{Path}` can't be added to the library because it doesn't exist", absolutePath);
            return Optional<LocalFile.ReadOnly>.None;
        }

        using var tx = _connection.BeginTransaction();
        var entityId = tx.TempId();

        var libraryFile = await AddLibraryFileAsync(tx, entityId, absolutePath, cancellationToken: cancellationToken);

        var localFile = new LocalFile.New(tx, entityId)
        {
            LibraryFile = libraryFile,
            OriginalPath = absolutePath.GetFullPath(),
        };

        var result = await tx.Commit();
        return result.Remap(localFile);
    }

    private async Task<LibraryFile.New> AddLibraryFileAsync(
        ITransaction tx,
        EntityId entityId,
        AbsolutePath filePath,
        CancellationToken cancellationToken)
    {
        var (hash, isArchive) = await AnalyzeFileAsync(filePath, cancellationToken: cancellationToken);

        var libraryFile = new LibraryFile.New(tx, entityId)
        {
            FileName = filePath.FileName,
            Hash = hash,
            Size = filePath.FileInfo.Size,
            LibraryItem = new LibraryItem.New(tx, entityId)
            {
                Name = filePath.FileName,
            },
        };

        if (!isArchive) return libraryFile;

        var libraryArchive = new LibraryArchive.New(tx, entityId)
        {
            LibraryFile = libraryFile,
        };

        await ExtractArchiveAsync(tx, libraryArchive, filePath, cancellationToken);
        return libraryFile;
    }

    private async ValueTask<(Hash hash, bool isArchive)> AnalyzeFileAsync(AbsolutePath filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);

        var isArchive = await _fileExtractor.CanExtract(stream);
        stream.Position = 0;

        // TODO: hash activity
        var hash = await stream.XxHash64Async(token: cancellationToken);
        stream.Position = 0;

        return (hash, isArchive);
    }

    private async ValueTask ExtractArchiveAsync(
        ITransaction tx,
        LibraryArchive.New libraryArchive,
        AbsolutePath archivePath,
        CancellationToken cancellationToken)
    {
        var streamFactory = new NativeFileStreamFactory(archivePath);

        // TODO: extract activity
        await using var tempDirectory = _temporaryFileManager.CreateFolder();
        await _fileExtractor.ExtractAllAsync(streamFactory, dest: tempDirectory, token: cancellationToken);

        var files = tempDirectory.Path.EnumerateFileEntries().ToArray();

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            CancellationToken = cancellationToken,
        }, async (file, innerCancellationToken) =>
        {
            var libraryFile = await AddLibraryFileAsync(tx, tx.TempId(), file.Path, innerCancellationToken);
            var archiveFileEntry = new LibraryArchiveFileEntry.New(tx, libraryFile.Id)
            {
                LibraryFile = libraryFile,
                ParentId = libraryArchive.Id,
            };
        });
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
