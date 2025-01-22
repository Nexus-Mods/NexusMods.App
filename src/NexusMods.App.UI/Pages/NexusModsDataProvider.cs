using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Resources;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.Collections;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Networking.NexusWebApi;
using R3;

namespace NexusMods.App.UI.Pages;
using CollectionDownloadEntity = Abstractions.NexusModsLibrary.Models.CollectionDownload;

public enum CollectionDownloadsFilter
{
    OnlyRequired,
    OnlyOptional,
}

[UsedImplicitly]
public class NexusModsDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly CollectionDownloader _collectionDownloader;
    private readonly IResourceLoader<EntityId, Bitmap> _thumbnailLoader;

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _collectionDownloader = new CollectionDownloader(serviceProvider);
        _serviceProvider = serviceProvider;
        _thumbnailLoader = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveCollectionItems(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        IObservable<CollectionDownloadsFilter> filterObservable,
        LoadoutId loadoutId)
    {
        var collectionGroupObservable = _collectionDownloader.GetCollectionGroupObservable(revisionMetadata, loadoutId).Replay(bufferSize: 1).RefCount();

        return _connection
            .ObserveDatoms(CollectionDownloadEntity.CollectionRevision, revisionMetadata)
            .AsEntityIds()
            .Transform(datom => CollectionDownloadEntity.Load(_connection.Db, datom.E))
            .FilterOnObservable(downloadEntity => filterObservable.Select(filter => filter switch
            {
                CollectionDownloadsFilter.OnlyRequired => !downloadEntity.IsOptional,
                CollectionDownloadsFilter.OnlyOptional => downloadEntity.IsOptional,
            }))
            .FilterImmutable(static downloadEntity => downloadEntity.IsCollectionDownloadNexusMods() || downloadEntity.IsCollectionDownloadExternal())
            .TransformOnObservable(IObservable<ILibraryItemModel> (downloadEntity) =>
            {
                if (downloadEntity.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
                {
                    return ToLibraryItemModelObservable(collectionGroupObservable, nexusModsDownload);
                }

                if (downloadEntity.TryGetAsCollectionDownloadExternal(out var externalDownload))
                {
                    return System.Reactive.Linq.Observable.Return(ToLibraryItemModel(collectionGroupObservable, externalDownload));
                }

                throw new UnreachableException();
            });
    }

    private IObservable<ILibraryItemModel> ToLibraryItemModelObservable(
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable,
        CollectionDownloadNexusMods.ReadOnly nexusModsDownload)
    {
        var statusObservable = _collectionDownloader.GetStatusObservable(nexusModsDownload.AsCollectionDownload(), groupObservable);

        return statusObservable.Select(ILibraryItemModel (status) =>
        {
            var (isDownloaded, isInstalled, isOptional) = (status.IsDownloaded(), status.IsInstalled(out _), nexusModsDownload.AsCollectionDownload().IsOptional);
            var showInstallable = (isDownloaded, isInstalled, isOptional) switch
            {
                (isDownloaded: false, _, _) => false,
                (_, isInstalled: true, _) => true,
                (_, isInstalled: false, isOptional: true) => true,
                _ => false,
            };

            if (showInstallable)
            {
                var isInstalledObservable = statusObservable.Select(static status => status.IsInstalled(out _)).ToObservable();
                var model = new NexusModsFileMetadataLibraryItemModel.Installable(nexusModsDownload, _serviceProvider)
                {
                    IsInstalledObservable = isInstalledObservable,
                };

                model.Name.Value = nexusModsDownload.FileMetadata.Name;
                model.Version.Value = nexusModsDownload.FileMetadata.Version;
                if (nexusModsDownload.FileMetadata.Size.TryGet(out var size))
                    model.ItemSize.Value = size;

                return model;
            }
            else
            {
                var downloadJobObservable = _jobMonitor.GetObservableChangeSet<NexusModsDownloadJob>()
                    .FilterImmutable(job =>
                    {
                        var definition = job.Definition as NexusModsDownloadJob;
                        Debug.Assert(definition is not null);
                        return definition.FileMetadata.Id == nexusModsDownload.FileMetadata;
                    })
                    .QueryWhenChanged(static query => query.Items.MaxBy(job => job.Status))
                    .ToObservable()
                    .Prepend((_jobMonitor, nexusModsDownload.FileMetadata), static state =>
                    {
                        var (jobMonitor, fileMetadata) = state;
                        if (jobMonitor.Jobs.TryGetFirst(job => job.Definition is NexusModsDownloadJob nexusModsDownloadJob && nexusModsDownloadJob.FileMetadata.Id == fileMetadata, out var job))
                            return job;
                        return null;
                    })
                    .WhereNotNull();

                var model = new NexusModsFileMetadataLibraryItemModel.Downloadable(nexusModsDownload, _serviceProvider)
                {
                    IsInLibraryObservable = statusObservable.Select(static status => status.IsDownloaded()).ToObservable(),
                    DownloadJobObservable = downloadJobObservable,
                };

                model.Name.Value = nexusModsDownload.FileMetadata.Name;
                model.Version.Value = nexusModsDownload.FileMetadata.Version;
                if (nexusModsDownload.FileMetadata.Size.TryGet(out var size))
                    model.ItemSize.Value = size;

                return model;
            }
        });
    }

    private ILibraryItemModel ToLibraryItemModel(
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable,
        CollectionDownloadExternal.ReadOnly externalDownload)
    {
        var isInLibraryObservable = _collectionDownloader.GetStatusObservable(externalDownload.AsCollectionDownload(), groupObservable)
            .Select(status => status.IsDownloaded())
            .ToObservable();

        var downloadJobObservable = _jobMonitor.GetObservableChangeSet<ExternalDownloadJob>()
            .FilterImmutable(job =>
            {
                var definition = job.Definition as ExternalDownloadJob;
                Debug.Assert(definition is not null);
                return definition.ExpectedMd5 == externalDownload.Md5;
            })
            .QueryWhenChanged(static query => query.Items.MaxBy(job => job.Status))
            .ToObservable()
            .Prepend((_jobMonitor, externalDownload), static state =>
            {
                var (jobMonitor, download) = state;
                if (jobMonitor.Jobs.TryGetFirst(job => job.Definition is ExternalDownloadJob externalDownloadJob && externalDownloadJob.ExpectedMd5 == download.Md5, out var job))
                    return job;
                return null;
            })
            .WhereNotNull();

        var model = new ExternalDownloadItemModel(externalDownload, _serviceProvider)
        {
            IsInLibraryObservable = isInLibraryObservable,
            DownloadJobObservable = downloadJobObservable,
        };

        model.Name.Value = externalDownload.AsCollectionDownload().Name;
        model.ItemSize.Value = externalDownload.Size;
        return model;
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveFlatLibraryItems(LibraryFilter libraryFilter)
    {
        // NOTE(erri120): For the flat library view, we display each NexusModsLibraryFile
        return NexusModsLibraryItem
            .ObserveAll(_connection)
            // only show library files for the currently selected game
            .FilterOnObservable((file, _) => libraryFilter.GameObservable.Select(game => file.ModPageMetadata.Uid.GameId.Equals(game.GameId)))
            .Transform((file, _) => ToLibraryItemModel(file, libraryFilter, true));
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveNestedLibraryItems(LibraryFilter libraryFilter)
    {
        // NOTE(erri120): For the nested library view, the parents are "fake" library
        // models that represent the Nexus Mods mod page, with each child being a
        // NexusModsLibraryFile that links to the mod page.
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            // only show mod pages for the currently selected game
            .FilterOnObservable((modPage, _) => libraryFilter.GameObservable.Select(game => modPage.Uid.GameId.Equals(game.GameId)))
            // only show mod pages that have library files
            .FilterOnObservable((_, e) => _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, e)
                .IsNotEmpty()
            )
            .Transform((modPage, _) => ToLibraryItemModel(modPage, libraryFilter));
    }

    private ILibraryItemModel ToLibraryItemModel(NexusModsLibraryItem.ReadOnly nexusModsLibraryItem, LibraryFilter libraryFilter, bool showThumbnails)
    {
        var linkedLoadoutItemsObservable = QueryHelper.GetLinkedLoadoutItems(_connection, nexusModsLibraryItem.Id, libraryFilter);

        var model = new NexusModsFileLibraryItemModel(nexusModsLibraryItem, _serviceProvider, showThumbnails)
        {
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };

        model.Name.Value = nexusModsLibraryItem.FileMetadata.Name;
        model.DownloadedDate.Value = nexusModsLibraryItem.GetCreatedAt();
        model.Version.Value = nexusModsLibraryItem.FileMetadata.Version;

        if (nexusModsLibraryItem.FileMetadata.Size.TryGet(out var size))
            model.ItemSize.Value = size;

        return model;
    }

    private ILibraryItemModel ToLibraryItemModel(
        NexusModsModPageMetadata.ReadOnly modPageMetadata,
        LibraryFilter libraryFilter)
    {
        // TODO: dispose
        var cache = new SourceCache<Datom, EntityId>(static datom => datom.E);
        var disposable = _connection
            .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPageMetadata.Id)
            .AsEntityIds()
            .Adapt(new SourceCacheAdapter<Datom, EntityId>(cache))
            .SubscribeWithErrorLogging();

        var hasChildrenObservable = cache.Connect().IsNotEmpty();
        var childrenObservable = cache.Connect().Transform((_, e) =>
        {
            var libraryFile = NexusModsLibraryItem.Load(_connection.Db, e);
            return ToLibraryItemModel(libraryFile, libraryFilter, false);
        });

        var linkedLoadoutItemsObservable = cache.Connect()
            // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
            .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
            .FilterInObservableLoadout(_connection, libraryFilter)
            .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e));

        var libraryFilesObservable = cache.Connect()
            .Transform((_, e) => NexusModsLibraryItem.Load(_connection.Db, e));

        var numInstalledObservable = cache.Connect().TransformOnObservable((_, e) => _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e)
            .AsEntityIds()
            .FilterInObservableLoadout(_connection, libraryFilter)
            .QueryWhenChanged(query => query.Count > 0)
            .Prepend(false)
        ).QueryWhenChanged(static query => query.Items.Count(static b => b));

        var model = new NexusModsModPageLibraryItemModel(libraryFilesObservable, _serviceProvider)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,

            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
            NumInstalledObservable = numInstalledObservable,
        };

        model.Name.Value = modPageMetadata.Name;
        return model;
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .FilterOnObservable((_, modPageEntityId) => _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPageEntityId)
                .FilterOnObservable((d, _) => _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, d.E)
                    .AsEntityIds()
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .IsNotEmpty())
                .IsNotEmpty()
            )
            .Transform(modPage =>
            {
                var linkedItemsObservable = _connection
                    .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPage.Id).AsEntityIds()
                    .FilterOnObservable((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).IsNotEmpty())
                    // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
                    .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .Transform(datom => LoadoutItem.Load(_connection.Db, datom.E))
                    .RefCount();

                var hasChildrenObservable = linkedItemsObservable.IsNotEmpty();
                var childrenObservable = linkedItemsObservable.Transform(loadoutItem => LoadoutDataProviderHelper.ToChildItemModel(_connection, loadoutItem));

                var parentItemModel = new CompositeItemModel<EntityId>(modPage.Id)
                {
                    HasChildrenObservable = hasChildrenObservable,
                    ChildrenObservable = childrenObservable,
                };

                parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: modPage.Name));
                parentItemModel.Add(SharedColumns.Name.ImageComponentKey, ImageComponent.FromPipeline(_thumbnailLoader, modPage.Id, initialValue: ImagePipelines.ModPageThumbnailFallback));

                LoadoutDataProviderHelper.AddDateComponent(parentItemModel, modPage.GetCreatedAt(), linkedItemsObservable);
                LoadoutDataProviderHelper.AddIsEnabled(_connection, parentItemModel, linkedItemsObservable);

                return parentItemModel;
            });
    }
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
