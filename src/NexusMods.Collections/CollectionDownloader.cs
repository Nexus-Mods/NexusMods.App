using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.CrossPlatform.Process;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using Reloaded.Memory.Extensions;

namespace NexusMods.Collections;

/// <summary>
/// Methods for collection downloads.
/// </summary>
[PublicAPI]
public class CollectionDownloader
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IConnection _connection;
    private readonly ILoginManager _loginManager;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly ILibraryService _libraryService;
    private readonly IOSInterop _osInterop;
    private readonly HttpClient _httpClient;
    private readonly IJobMonitor _jobMonitor;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CollectionDownloader(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<CollectionDownloader>>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
    }

    private async ValueTask<bool> CanDirectDownload(CollectionDownloadExternal.ReadOnly download, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Testing if `{Uri}` can be downloaded directly", download.Uri);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, download.Uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("application/"))
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because Content-Type `{ContentType}` doesn't indicate a binary download", download.Uri, contentType);
                return false;
            }

            if (!response.Content.Headers.ContentLength.HasValue)
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because the response doesn't have a Content-Length", download.Uri);
                return false;
            }

            var size = Size.FromLong(response.Content.Headers.ContentLength.Value);
            if (size != download.Size)
            {
                _logger.LogWarning("Download at `{Uri}` can't be downloaded automatically because the Content-Length `{ContentLength}` doesn't match the expected size `{ExpectedSize}`", download.Uri, size, download.Size);
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while checking if `{Uri}` can be downloaded directly", download.Uri);
            return false;
        }
    }

    /// <summary>
    /// Downloads an external file.
    /// </summary>
    public async ValueTask Download(CollectionDownloadExternal.ReadOnly download, bool onlyDirectDownloads, CancellationToken cancellationToken)
    {
        if (await CanDirectDownload(download, cancellationToken))
        {
            _logger.LogInformation("Downloading external file at `{Uri}` directly", download.Uri);
            var job = ExternalDownloadJob.Create(_serviceProvider, download.Uri, download.Md5, download.AsCollectionDownload().Name);
            await _libraryService.AddDownload(job);
        }
        else
        {
            if (onlyDirectDownloads) return;

            _logger.LogInformation("Unable to direct download `{Uri}`, using browse as a fallback", download.Uri);
            await _osInterop.OpenUrl(download.Uri, logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Downloads an external file or opens the browser if the file can't be downloaded automatically.
    /// </summary>
    public ValueTask Download(CollectionDownloadExternal.ReadOnly download, CancellationToken cancellationToken)
    {
        return Download(download, onlyDirectDownloads: false, cancellationToken);
    }

    /// <summary>
    /// Downloads a file from nexus mods for premium users or opens the download page in the browser.
    /// </summary>
    public async ValueTask Download(CollectionDownloadNexusMods.ReadOnly download, CancellationToken cancellationToken)
    {
        if (_loginManager.IsPremium)
        {
            await using var tempPath = _temporaryFileManager.CreateFile();
            var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, download.FileMetadata, cancellationToken: cancellationToken);
            await _libraryService.AddDownload(job);
        }
        else
        {
            await _osInterop.OpenUrl(download.FileMetadata.GetUri(), logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Checks whether the item was already downloaded.
    /// </summary>
    public static bool IsDownloaded(CollectionDownloadExternal.ReadOnly download, IDb db) => TryGetDownloadedItem(download, db, out _);

    /// <summary>
    /// Tries to get the downloaded item.
    /// </summary>
    public static bool TryGetDownloadedItem(CollectionDownloadExternal.ReadOnly download, IDb db, out LibraryFile.ReadOnly item)
    {
        var directDownloadDatoms = db.Datoms(DirectDownloadLibraryFile.Md5, download.Md5);
        if (directDownloadDatoms.Count > 0)
        {
            foreach (var datom in directDownloadDatoms)
            {
                var file = DirectDownloadLibraryFile.Load(db, datom.E);
                if (file.IsValid())
                {
                    item = file.AsLibraryFile();
                    return true;
                }
            }
        }

        var locallyAddedDatoms = db.Datoms(LocalFile.Md5, download.Md5);
        if (locallyAddedDatoms.Count > 0)
        {
            foreach (var datom in locallyAddedDatoms)
            {
                var file = LocalFile.Load(db, datom.E);
                if (file.IsValid())
                {
                    item = file.AsLibraryFile();
                    return true;
                }
            }
        }

        item = default(LibraryFile.ReadOnly);
        return false;
    }

    /// <summary>
    /// Returns an observable with values whether the external file has been downloaded.
    /// </summary>
    public static IObservable<bool> IsDownloadedObservable(IConnection connection, CollectionDownloadExternal.ReadOnly download)
    {
        var hasDirectDownloads = connection.ObserveDatoms(SliceDescriptor.Create(DirectDownloadLibraryFile.Md5, download.Md5, connection.AttributeCache)).IsNotEmpty();
        var hasLocallyAdded = connection.ObserveDatoms(SliceDescriptor.Create(LocalFile.Md5, download.Md5, connection.AttributeCache)).IsNotEmpty();

        return hasDirectDownloads.CombineLatest(hasLocallyAdded, (a, b) => a || b);
    }

    /// <summary>
    /// Checks whether the item was already downloaded.
    /// </summary>
    public static bool IsDownloaded(CollectionDownloadNexusMods.ReadOnly download, IDb db) => TryGetDownloadedItem(download, db, out _);

    /// <summary>
    /// Tries to get the downloaded item.
    /// </summary>
    public static bool TryGetDownloadedItem(CollectionDownloadNexusMods.ReadOnly download, IDb db, out NexusModsLibraryItem.ReadOnly item)
    {
        var datoms = db.Datoms(NexusModsLibraryItem.FileMetadata, download.FileMetadata);
        if (datoms.Count == 0)
        {
            item = default(NexusModsLibraryItem.ReadOnly);
            return false;
        }

        foreach (var datom in datoms)
        {
            item = NexusModsLibraryItem.Load(db, datom.E);
            if (item.IsValid()) return true;
        }

        item = default(NexusModsLibraryItem.ReadOnly);
        return false;
    }

    /// <summary>
    /// Returns an observable with values whether the Nexus Mods file has been downloaded.
    /// </summary>
    public static IObservable<bool> IsDownloadedObservable(IConnection connection, CollectionDownloadNexusMods.ReadOnly download)
    {
        return connection.ObserveDatoms(NexusModsLibraryItem.FileMetadata, download.FileMetadata).IsNotEmpty().DistinctUntilChanged();
    }

    /// <summary>
    /// Returns an observable with values whether the file has been downloaded.
    /// </summary>
    public static IObservable<bool> IsDownloadedObservable(IConnection connection, CollectionDownload.ReadOnly download)
    {
        if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
        {
            return IsDownloadedObservable(connection, nexusModsDownload);
        }

        if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
        {
            return IsDownloadedObservable(connection, externalDownload);
        }

        throw new UnreachableException();
    }

    /// <summary>
    /// Returns an observable with the number of downloaded items.
    /// </summary>
    public IObservable<int> DownloadedItemCountObservable(CollectionRevisionMetadata.ReadOnly revisionMetadata, ItemType itemType)
    {
        return _connection
            .ObserveDatoms(CollectionDownload.CollectionRevision, revisionMetadata)
            .AsEntityIds()
            .Transform(datom => CollectionDownload.Load(_connection.Db, datom.E))
            .FilterImmutable(download => DownloadMatchesItemType(download, itemType))
            .FilterImmutable(static download => download.IsCollectionDownloadNexusMods() || download.IsCollectionDownloadExternal())
            .TransformOnObservable(download => IsDownloadedObservable(_connection, download))
            .FilterImmutable(static isDownloaded => isDownloaded)
            .Count()
            .Prepend(0);
    }

    /// <summary>
    /// Counts the items.
    /// </summary>
    public int CountItems(CollectionRevisionMetadata.ReadOnly revisionMetadata, ItemType itemType)
    {
        return revisionMetadata.Downloads
            .Where(download => DownloadMatchesItemType(download, itemType))
            .Count(download => download.IsCollectionDownloadNexusMods() || download.IsCollectionDownloadExternal());
    }

    /// <summary>
    /// Returns whether the item matches the given item type.
    /// </summary>
    internal static bool DownloadMatchesItemType(CollectionDownload.ReadOnly download, ItemType itemType)
    {
        if (download.IsOptional && itemType.HasFlagFast(ItemType.Optional)) return true;
        if (download.IsRequired && itemType.HasFlagFast(ItemType.Required)) return true;
        return false;
    }

    /// <summary>
    /// Checks whether the items in the collection were downloaded.
    /// </summary>
    public static bool IsFullyDownloaded(CollectionDownload.ReadOnly[] items, IDb db)
    {
        return items.All(download => IsDownloaded(download, db));
    }

    /// <summary>
    /// Checks whether the item was already downloaded.
    /// </summary>
    public static bool IsDownloaded(CollectionDownload.ReadOnly download, IDb db)
    {
        if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
        {
            return IsDownloaded(nexusModsDownload, db);
        }

        if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
        {
            return IsDownloaded(externalDownload, db);
        }

        if (download.IsCollectionDownloadBundled()) return true;
        return false;
    }

    [Flags, PublicAPI]
    public enum ItemType
    {
        Required = 1,
        Optional = 2,
    };

    /// <summary>
    /// Downloads everything in the revision.
    /// </summary>
    public async ValueTask DownloadItems(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        ItemType itemType,
        IDb db,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        var job = new DownloadCollectionJob
        {
            Downloader = this,
            RevisionMetadata = revisionMetadata,
            Db = db,
            ItemType = itemType,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };

        await _jobMonitor.Begin<DownloadCollectionJob, R3.Unit>(job);
    }

    /// <summary>
    /// Checks whether the collection is installed.
    /// </summary>
    public IObservable<bool> IsCollectionInstalled(CollectionRevisionMetadata.ReadOnly revision)
    {
        return _connection.ObserveDatoms(NexusCollectionLoadoutGroup.Revision, revision).IsNotEmpty();
    }

    /// <summary>
    /// Deletes all associated collection loadout groups.
    /// </summary>
    public async ValueTask DeleteCollectionLoadoutGroup(CollectionRevisionMetadata.ReadOnly revision, CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        var groupDatoms = db.Datoms(NexusCollectionLoadoutGroup.Revision, revision);
        foreach (var datom in groupDatoms)
        {
            tx.Delete(datom.E, recursive: true);
        }

        await tx.Commit();
    }

    /// <summary>
    /// Returns all items of the desired type (required/optional).
    /// </summary>
    public CollectionDownload.ReadOnly[] GetItems(CollectionRevisionMetadata.ReadOnly revision, ItemType itemType)
    {
        var res = new CollectionDownload.ReadOnly[revision.Downloads.Count];

        var i = 0;
        foreach (var download in revision.Downloads)
        {
            if (!DownloadMatchesItemType(download, itemType)) continue;
            res[i++] = download;
        }

        Array.Resize(ref res, newSize: i);
        return res;
    }

    /// <summary>
    /// Gets the library file for the collection.
    /// </summary>
    public NexusModsCollectionLibraryFile.ReadOnly GetLibraryFile(CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        var datoms = _connection.Db.Datoms(
            (NexusModsCollectionLibraryFile.CollectionSlug, revisionMetadata.Collection.Slug),
            (NexusModsCollectionLibraryFile.CollectionRevisionNumber, revisionMetadata.RevisionNumber)
        );

        if (datoms.Count == 0) throw new Exception($"Unable to find collection file for revision `{revisionMetadata.Collection.Slug}` (`{revisionMetadata.RevisionNumber}`)");
        var source = NexusModsCollectionLibraryFile.Load(_connection.Db, datoms[0]);
        return source;
    }
}
