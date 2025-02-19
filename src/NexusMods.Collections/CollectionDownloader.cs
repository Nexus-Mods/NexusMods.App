using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.Process;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using OneOf;
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

    /// <summary>
    /// Gets or adds a revision.
    /// </summary>
    public async ValueTask<CollectionRevisionMetadata.ReadOnly> GetOrAddRevision(CollectionSlug slug, RevisionNumber revisionNumber, CancellationToken cancellationToken)
    {
        var revisions = CollectionRevisionMetadata
            .FindByRevisionNumber(_connection.Db, revisionNumber)
            .Where(r => r.Collection.Slug == slug);

        if (revisions.TryGetFirst(out var revision)) return revision;

        await using var destination = _temporaryFileManager.CreateFile();
        var downloadJob = _nexusModsLibrary.CreateCollectionDownloadJob(destination, slug, revisionNumber, CancellationToken.None);

        var libraryFile = await _libraryService.AddDownload(downloadJob);

        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");

        revision = await _nexusModsLibrary.GetOrAddCollectionRevision(collectionFile, slug, revisionNumber, cancellationToken);
        return revision;
    }

    record DirectDownloadResult(bool CanDownload, Optional<RelativePath> FileName = default)
    {
        public static readonly DirectDownloadResult Unable = new(CanDownload: false);
    };

    private async ValueTask<DirectDownloadResult> CanDirectDownload(
        CollectionDownloadExternal.ReadOnly download,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Testing if `{Uri}` can be downloaded directly", download.Uri);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, download.Uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode) return DirectDownloadResult.Unable;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("application/"))
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because Content-Type `{ContentType}` doesn't indicate a binary download", download.Uri, contentType);
                return DirectDownloadResult.Unable;
            }

            if (!response.Content.Headers.ContentLength.HasValue)
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because the response doesn't have a Content-Length", download.Uri);
                return DirectDownloadResult.Unable;
            }

            var size = Size.FromLong(response.Content.Headers.ContentLength.Value);
            if (size != download.Size)
            {
                _logger.LogWarning("Download at `{Uri}` can't be downloaded automatically because the Content-Length `{ContentLength}` doesn't match the expected size `{ExpectedSize}`", download.Uri, size, download.Size);
                return DirectDownloadResult.Unable;
            }

            var contentDispositionFileName = response.Content.Headers.ContentDisposition?.FileName;
            var fileName = contentDispositionFileName is null ? Optional<RelativePath>.None : RelativePath.FromUnsanitizedInput(contentDispositionFileName);

            return new DirectDownloadResult(CanDownload: true, FileName: fileName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while checking if `{Uri}` can be downloaded directly", download.Uri);
            return DirectDownloadResult.Unable;
        }
    }

    /// <summary>
    /// Downloads an external file.
    /// </summary>
    public async ValueTask Download(CollectionDownloadExternal.ReadOnly download, bool onlyDirectDownloads, CancellationToken cancellationToken)
    {
        var result = await CanDirectDownload(download, cancellationToken);
        if (result.CanDownload)
        {
            _logger.LogInformation("Downloading external file at `{Uri}` directly", download.Uri);
            var job = ExternalDownloadJob.Create(
                _serviceProvider,
                download.Uri,
                download.Md5,
                logicalFileName: download.AsCollectionDownload().Name,
                fileName: result.FileName
            );

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
    /// Returns an observable with the number of downloaded items.
    /// </summary>
    public IObservable<int> DownloadedItemCountObservable(CollectionRevisionMetadata.ReadOnly revisionMetadata, ItemType itemType)
    {
        return _connection
            .ObserveDatoms(CollectionDownload.CollectionRevision, revisionMetadata)
            .AsEntityIds()
            .Transform(datom => CollectionDownload.Load(_connection.Db, datom.E))
            .FilterImmutable(download => DownloadMatchesItemType(download, itemType))
            .TransformOnObservable(download => GetStatusObservable(download, Observable.Return(Optional<CollectionGroup.ReadOnly>.None)))
            .FilterImmutable(static status => status.IsDownloaded() && !status.IsBundled())
            .QueryWhenChanged(query => query.Count)
            .Prepend(0);
    }

    /// <summary>
    /// Counts the items.
    /// </summary>
    public static int CountItems(CollectionRevisionMetadata.ReadOnly revisionMetadata, ItemType itemType)
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
        return items.All(download => GetStatus(download, db).IsDownloaded());
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
    public IObservable<bool> IsCollectionInstalledObservable(
        CollectionRevisionMetadata.ReadOnly revision, 
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable, 
        ItemType itemType = ItemType.Required)
    {
        var observables = revision.Downloads
            .Where(download => DownloadMatchesItemType(download, itemType))
            .Select(download => GetStatusObservable(download, groupObservable).Select(static status => status.IsInstalled(out _)))
            .ToArray();

        if (observables.Length == 0) return groupObservable.Select(static optional => optional.HasValue);
        return observables.CombineLatest(static list => list.All(static installed => installed));
    }

    private static CollectionDownloadStatus GetStatus(CollectionDownloadBundled.ReadOnly download, Optional<CollectionGroup.ReadOnly> collectionGroup, IDb db)
    {
        if (!collectionGroup.HasValue) return new CollectionDownloadStatus.Bundled();

        var entityIds = db.Datoms(
            (NexusCollectionBundledLoadoutGroup.BundleDownload, download),
            (LoadoutItem.ParentId, collectionGroup.Value)
        );

        foreach (var entityId in entityIds)
        {
            var loadoutItem = LoadoutItem.Load(db, entityId);
            if (loadoutItem.IsValid()) return new CollectionDownloadStatus.Installed(loadoutItem);
        }

        return new CollectionDownloadStatus.Bundled();
    }

    private IObservable<CollectionDownloadStatus> GetStatusObservable(
        CollectionDownloadBundled.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        return _connection
            .ObserveDatoms(NexusCollectionBundledLoadoutGroup.BundleDownload, download)
            .TransformImmutable(datom => LoadoutItem.Load(_connection.Db, datom.E))
            .FilterOnObservable(item =>
            {
                return groupObservable
                    .Select(optional => optional.Convert(static group => group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId))
                    .Select(loadoutId => loadoutId.HasValue && item.LoadoutId == loadoutId.Value);
            })
            .QueryWhenChanged(query => query.Items.FirstOrOptional(static _ => true))
            .Select(optional =>
            {
                if (!optional.HasValue) return (CollectionDownloadStatus) new CollectionDownloadStatus.Bundled();
                return new CollectionDownloadStatus.Installed(optional.Value);
            })
            .Prepend(new CollectionDownloadStatus.Bundled());
    }

    private static CollectionDownloadStatus GetStatus(CollectionDownloadNexusMods.ReadOnly download, Optional<CollectionGroup.ReadOnly> collectionGroup, IDb db)
    {
        var datoms = db.Datoms(NexusModsLibraryItem.FileMetadata, download.FileMetadata);
        if (datoms.Count == 0) return new CollectionDownloadStatus.NotDownloaded();

        var libraryItem = default(NexusModsLibraryItem.ReadOnly);
        foreach (var datom in datoms)
        {
            libraryItem = NexusModsLibraryItem.Load(db, datom.E);
            if (libraryItem.IsValid()) break;
        }

        if (!libraryItem.IsValid()) return new CollectionDownloadStatus.NotDownloaded();
        return GetStatus(libraryItem.AsLibraryItem(), collectionGroup, db);
    }

    private IObservable<CollectionDownloadStatus> GetStatusObservable(
        CollectionDownloadNexusMods.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        return _connection
            .ObserveDatoms(NexusModsLibraryItem.FileMetadata, download.FileMetadata)
            .QueryWhenChanged(query => query.Items.FirstOrOptional(static _ => true))
            .DistinctUntilChanged(OptionalDatomComparer.Instance)
            .SelectMany(optional =>
            {
                if (!optional.HasValue) return Observable.Return<CollectionDownloadStatus>(new CollectionDownloadStatus.NotDownloaded());

                var libraryItem = LibraryItem.Load(_connection.Db, optional.Value.E);
                Debug.Assert(libraryItem.IsValid());

                return GetStatusObservable(libraryItem, groupObservable);
            });
    }

    private static CollectionDownloadStatus GetStatus(CollectionDownloadExternal.ReadOnly download, Optional<CollectionGroup.ReadOnly> collectionGroup, IDb db)
    {
        var datoms = db.Datoms(LibraryFile.Md5, download.Md5);
        if (datoms.Count == 0) return new CollectionDownloadStatus.NotDownloaded();

        foreach (var datom in datoms)
        {
            var libraryFile = DirectDownloadLibraryFile.Load(db, datom.E).AsLocalFile().AsLibraryFile();
            if (libraryFile.IsValid()) return GetStatus(libraryFile.AsLibraryItem(), collectionGroup, db);
        }

        return new CollectionDownloadStatus.NotDownloaded();
    }

    private IObservable<CollectionDownloadStatus> GetStatusObservable(
        CollectionDownloadExternal.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        var observable = _connection.ObserveDatoms(SliceDescriptor.Create(LibraryFile.Md5, download.Md5, _connection.AttributeCache));

        return observable
            .QueryWhenChanged(query => query.Items.FirstOrOptional(static _ => true))
            .Prepend(Optional<Datom>.None)
            .DistinctUntilChanged(OptionalDatomComparer.Instance)
            .SelectMany(optional =>
            {
                if (!optional.HasValue) return Observable.Return<CollectionDownloadStatus>(new CollectionDownloadStatus.NotDownloaded());

                var libraryItem = LibraryItem.Load(_connection.Db, optional.Value.E);
                Debug.Assert(libraryItem.IsValid());

                return GetStatusObservable(libraryItem, groupObservable);
            });
    }

    private static CollectionDownloadStatus GetStatus(
        LibraryItem.ReadOnly libraryItem,
        Optional<CollectionGroup.ReadOnly> collectionGroup,
        IDb db)
    {
        if (!collectionGroup.HasValue) return new CollectionDownloadStatus.InLibrary(libraryItem);

        var entityIds = db.Datoms(
            (LibraryLinkedLoadoutItem.LibraryItem, libraryItem),
            (LoadoutItem.ParentId, collectionGroup.Value)
        );

        if (entityIds.Count == 0) return new CollectionDownloadStatus.InLibrary(libraryItem);

        foreach (var entityId in entityIds)
        {
            var loadoutItem = LoadoutItem.Load(db, entityId);
            if (!loadoutItem.IsValid()) continue;
            return new CollectionDownloadStatus.Installed(loadoutItem);
        }

        return new CollectionDownloadStatus.InLibrary(libraryItem);
    }

    private IObservable<CollectionDownloadStatus> GetStatusObservable(
        LibraryItem.ReadOnly libraryItem,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        return _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryItem.LibraryItemId)
            .TransformImmutable(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E))
            .FilterOnObservable(item =>
            {
                return groupObservable
                    .Select(group =>
                    {
                        if (!group.HasValue) return false;
                        var itemLoadoutId = LoadoutItem.LoadoutId.Get(item);
                        var groupLoadoutId = LoadoutItem.LoadoutId.Get(group.Value);
                        var parentId = LoadoutItem.ParentId.Get(item);
                        var id = group.Value.Id;

                        return itemLoadoutId == groupLoadoutId && parentId == id;
                    });
            })
            .QueryWhenChanged(query =>
            {
                var optional = query.Items.FirstOrOptional(static x => true);

                CollectionDownloadStatus status = optional.HasValue
                    ? new CollectionDownloadStatus.Installed(optional.Value.AsLoadoutItemGroup().AsLoadoutItem())
                    : new CollectionDownloadStatus.InLibrary(libraryItem);

                return status;
            })
            .Prepend(new CollectionDownloadStatus.InLibrary(libraryItem));
    }

    /// <summary>
    /// Gets the status of a download as an observable.
    /// </summary>
    public IObservable<CollectionDownloadStatus> GetStatusObservable(
        CollectionDownload.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        if (download.TryGetAsCollectionDownloadBundled(out var bundled))
        {
            return GetStatusObservable(bundled, groupObservable).DistinctUntilChanged();
        }

        if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
        {
            return GetStatusObservable(nexusModsDownload, groupObservable).DistinctUntilChanged();
        }

        if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
        {
            return GetStatusObservable(externalDownload, groupObservable).DistinctUntilChanged();
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the status of a download.
    /// </summary>
    public static CollectionDownloadStatus GetStatus(CollectionDownload.ReadOnly download, IDb db)
    {
        return GetStatus(download, new Optional<CollectionGroup.ReadOnly>(), db);
    }

    /// <summary>
    /// Gets the status of a download.
    /// </summary>
    public static CollectionDownloadStatus GetStatus(
        CollectionDownload.ReadOnly download,
        Optional<CollectionGroup.ReadOnly> collectionGroup,
        IDb db)
    {
        if (download.TryGetAsCollectionDownloadBundled(out var bundled))
        {
            return GetStatus(bundled, collectionGroup, db);
        }

        if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
        {
            return GetStatus(nexusModsDownload, collectionGroup, db);
        }

        if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
        {
            return GetStatus(externalDownload, collectionGroup, db);
        }

        throw new NotSupportedException();
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
    public static CollectionDownload.ReadOnly[] GetItems(CollectionRevisionMetadata.ReadOnly revision, ItemType itemType)
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

    /// <summary>
    /// Returns the collection group associated with the revision or none.
    /// </summary>
    public static Optional<NexusCollectionLoadoutGroup.ReadOnly> GetCollectionGroup(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        LoadoutId loadoutId,
        IDb db)
    {
        var entityIds = db.Datoms(
            (NexusCollectionLoadoutGroup.Revision, revisionMetadata),
            (LoadoutItem.Loadout, loadoutId)
        );

        if (entityIds.Count == 0) return Optional.None<NexusCollectionLoadoutGroup.ReadOnly>();
        foreach (var entityId in entityIds)
        {
            var group = NexusCollectionLoadoutGroup.Load(db, entityId);
            if (group.IsValid()) return group;
        }

        return new Optional<NexusCollectionLoadoutGroup.ReadOnly>();
    }

    /// <summary>
    /// Gets an observable stream containing the collection group associated with the revision.
    /// </summary>
    public IObservable<Optional<CollectionGroup.ReadOnly>> GetCollectionGroupObservable(CollectionRevisionMetadata.ReadOnly revision, LoadoutId targetLoadout)
    {
        return _connection
            .ObserveDatoms(NexusCollectionLoadoutGroup.Revision, revision)
            .QueryWhenChanged(query =>
            {
                foreach (var datom in query.Items)
                {
                    var group = CollectionGroup.Load(_connection.Db, datom.E);
                    if (!group.IsValid()) continue;
                    if (group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId != targetLoadout) continue;
                    return Optional<CollectionGroup.ReadOnly>.Create(group);
                }

                return Optional<CollectionGroup.ReadOnly>.None;
            })
            .Prepend(GetCollectionGroup(revision, targetLoadout, _connection.Db).Convert(static x => x.AsCollectionGroup()));
    }

    /// <summary>
    /// Deletes a revision and all downloaded entities.
    /// </summary>
    public async ValueTask DeleteRevision(CollectionRevisionMetadataId revisionId)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        var downloadIds = db.Datoms(CollectionDownload.CollectionRevision, revisionId);
        foreach (var downloadId in downloadIds)
        {
            tx.Delete(downloadId.E, recursive: false);
        }

        tx.Delete(revisionId, recursive: false);

        await tx.Commit();
    }

    /// <summary>
    /// Deletes a collection, all revisions, and all download entities of all revisions.
    /// </summary>
    public async ValueTask DeleteCollection(CollectionMetadataId collectionMetadataId)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        var revisionIds = db.Datoms(CollectionRevisionMetadata.CollectionId, collectionMetadataId);
        foreach (var revisionId in revisionIds)
        {
            var downloadIds = db.Datoms(CollectionDownload.CollectionRevision, revisionId.E);
            foreach (var downloadId in downloadIds)
            {
                tx.Delete(downloadId.E, recursive: false);
            }

            tx.Delete(revisionId.E, recursive: false);
        }

        tx.Delete(collectionMetadataId, recursive: false);

        await tx.Commit();
    }
}

/// <summary>
/// Represents the current status of a download in a collection.
/// </summary>
[PublicAPI]
[DebuggerDisplay("{Value}")]
public readonly struct CollectionDownloadStatus : IEquatable<CollectionDownloadStatus>
{
    /// <summary>
    /// Value.
    /// </summary>
    public readonly OneOf<NotDownloaded, Bundled, InLibrary, Installed> Value;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CollectionDownloadStatus(OneOf<NotDownloaded, Bundled, InLibrary, Installed> value)
    {
        Value = value;
    }

    /// <summary>
    /// Item hasn't been downloaded yet.
    /// </summary>
    public readonly struct NotDownloaded;

    /// <summary>
    /// For bundled downloads.
    /// </summary>
    public readonly struct Bundled;

    /// <summary>
    /// For items that have been downloaded and added to the library.
    /// </summary>
    public readonly struct InLibrary : IEquatable<InLibrary>
    {
        /// <summary>
        /// The library item.
        /// </summary>
        public readonly LibraryItem.ReadOnly LibraryItem;

        /// <summary>
        /// Constructor.
        /// </summary>
        public InLibrary(LibraryItem.ReadOnly libraryItem)
        {
            LibraryItem = libraryItem;
        }

        /// <inheritdoc/>
        public bool Equals(InLibrary other) => LibraryItem.LibraryItemId == other.LibraryItem.LibraryItemId;
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is InLibrary other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => LibraryItem.Id.GetHashCode();
    }

    /// <summary>
    /// For items that have been installed.
    /// </summary>
    public readonly struct Installed : IEquatable<Installed>
    {
        /// <summary>
        /// The loadout item.
        /// </summary>
        public readonly LoadoutItem.ReadOnly LoadoutItem;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Installed(LoadoutItem.ReadOnly loadoutItem)
        {
            LoadoutItem = loadoutItem;
        }

        /// <inheritdoc/>
        public bool Equals(Installed other) => LoadoutItem.LoadoutItemId == other.LoadoutItem.LoadoutItemId;
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Installed other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => LoadoutItem.Id.GetHashCode();
    }

    public bool IsNotDownloaded() => Value.IsT0;
    public bool IsDownloaded() => !IsNotDownloaded();
    public bool IsBundled() => Value.IsT1;

    public bool IsInLibrary(out LibraryItem.ReadOnly libraryItem)
    {
        if (!Value.TryPickT2(out var value, out _))
        {
            libraryItem = default(LibraryItem.ReadOnly);
            return false;
        }

        libraryItem = value.LibraryItem;
        return true;
    }

    public bool IsInstalled(out LoadoutItem.ReadOnly loadoutItem)
    {
        if (!Value.TryPickT3(out var value, out _))
        {
            loadoutItem = default(LoadoutItem.ReadOnly);
            return false;
        }

        loadoutItem = value.LoadoutItem;
        return true;
    }

    public static implicit operator CollectionDownloadStatus(NotDownloaded x) => new(x);
    public static implicit operator CollectionDownloadStatus(Bundled x) => new(x);
    public static implicit operator CollectionDownloadStatus(InLibrary x) => new(x);
    public static implicit operator CollectionDownloadStatus(Installed x) => new(x);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CollectionDownloadStatus other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(CollectionDownloadStatus other)
    {
        var (index, otherIndex) = (Value.Index, other.Value.Index);
        if (index != otherIndex) return false;

        if (IsNotDownloaded()) return true;
        if (IsBundled()) return true;

        if (Value.TryPickT2(out var inLibrary, out _))
        {
            return inLibrary.Equals(other.Value.AsT2);
        }

        if (Value.TryPickT3(out var installed, out _))
        {
            return installed.Equals(other.Value.AsT3);
        }

        throw new UnreachableException();
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();
}

file class DatomEntityIdEqualityComparer : IEqualityComparer<Datom>
{
    public static readonly IEqualityComparer<Datom> Instance = new DatomEntityIdEqualityComparer();

    public bool Equals(Datom x, Datom y)
    {
        return x.E == y.E;
    }

    public int GetHashCode(Datom obj)
    {
        return obj.E.GetHashCode();
    }
}

internal class OptionalDatomComparer : IEqualityComparer<Optional<Datom>>
{
    public static readonly IEqualityComparer<Optional<Datom>> Instance = new OptionalDatomComparer();

    public bool Equals(Optional<Datom> x, Optional<Datom> y)
    {
        var (a, b) = (x.HasValue, y.HasValue);
        return (a, b) switch
        {
            (false, false) => true,
            (false, true) => false,
            (true, false) => false,
            (true, true) => x.Value.E.Equals(y.Value.E),
        };
    }

    public int GetHashCode(Optional<Datom> datom)
    {
        return datom.GetHashCode();
    }
}
