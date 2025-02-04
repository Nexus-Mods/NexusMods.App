using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
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
using NuGet.Versioning;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages;
using CollectionDownloadEntity = Abstractions.NexusModsLibrary.Models.CollectionDownload;

public enum CollectionDownloadsFilter
{
    OnlyRequired,
    OnlyOptional,
}

public class NexusModsDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;
    private readonly IModUpdateService _modUpdateService;
    private readonly CollectionDownloader _collectionDownloader;
    private readonly IResourceLoader<EntityId, Bitmap> _thumbnailLoader;

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _connection = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _modUpdateService = serviceProvider.GetRequiredService<IModUpdateService>();

        _collectionDownloader = new CollectionDownloader(serviceProvider);
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

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter)
    {
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            // only show mod pages for the currently selected game
            .FilterOnObservable(modPage => libraryFilter.GameObservable.Select(game => modPage.Uid.GameId.Equals(game.GameId)))
            // only show mod pages that have library files
            .FilterOnObservable(modPage => _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadata, modPage)
                .IsNotEmpty()
            )
            .Transform(modPage => ToLibraryItemModel(modPage, libraryFilter));
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(NexusModsModPageMetadata.ReadOnly modPage, LibraryFilter libraryFilter)
    {
        var libraryItems = _connection
            .ObserveDatoms(NexusModsLibraryItem.ModPageMetadata, modPage)
            .AsEntityIds()
            .Transform(datom => NexusModsLibraryItem.Load(_connection.Db, datom.E))
            .RefCount();

        var linkedLoadoutItemsObservable = libraryItems.MergeManyChangeSets(libraryItem => LibraryDataProviderHelper.GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id));

        var hasChildrenObservable = libraryItems.IsNotEmpty();
        var childrenObservable = libraryItems.Transform(libraryItem => ToLibraryItemModel(libraryItem, libraryFilter));

        var parentItemModel = new CompositeItemModel<EntityId>(modPage.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: modPage.Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, ImageComponent.FromPipeline(_thumbnailLoader, modPage.Id, initialValue: ImagePipelines.ModPageThumbnailFallback));

        // Size: sum of library files
        var sizeObservable = libraryItems
            .TransformImmutable(static item => LibraryFile.Size.GetOptional(item).ValueOr(() => Size.Zero))
            .ForAggregation()
            .Sum(static size => (long)size.Value)
            .Select(static size => Size.FromLong(size));

        parentItemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(
            initialValue: Size.Zero,
            valueObservable: sizeObservable
        ));

        // Downloaded date: most recent downloaded file date
        var downloadedDateObservable = libraryItems
            .TransformImmutable(static item => item.GetCreatedAt())
            .QueryWhenChanged(query => query.Items.OptionalMaxBy(item => item).ValueOr(DateTimeOffset.MinValue));

        parentItemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(
            initialValue: modPage.GetCreatedAt(),
            valueObservable: downloadedDateObservable
        ));

        // Version: highest version number
        var currentVersionObservable = libraryItems
            .TransformImmutable(static item =>
            {
                // NOTE(erri120, sewer): simplest version parsing for now
                var rawVersion = item.FileMetadata.Version;
                _ = NuGetVersion.TryParse(rawVersion, out var parsedVersion);

                return (rawVersion, parsedVersion: Optional<NuGetVersion>.Create(parsedVersion));
            })
            .QueryWhenChanged(static query =>
            {
                var max = query.Items.OptionalMaxBy(static tuple => tuple.parsedVersion.ValueOr(new NuGetVersion(0, 0, 0)));
                if (!max.HasValue) return string.Empty;
                return max.Value.rawVersion;
            });

        parentItemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(
            initialValue: string.Empty,
            valueObservable: currentVersionObservable
        ));

        // Update available
        var newestVersionObservable = _modUpdateService
            .GetNewestModPageVersionObservable(modPage)
            .Select(static optional => optional.Convert(static files => files.files.First().Version));

        parentItemModel.AddObservable(
            key: LibraryColumns.ItemVersion.NewVersionComponentKey,
            observable: newestVersionObservable,
            componentFactory: static (valueObservable, initialValue) => new StringComponent(
                initialValue,
                valueObservable
            )
        );

        // Update button
        var newestFiles = _modUpdateService.GetNewestModPageVersionObservable(modPage);

        parentItemModel.AddObservable(
            key: LibraryColumns.Actions.UpdateComponentKey,
            observable: newestFiles,
            componentFactory: static (valueObservable, initialValue) => new LibraryComponents.UpdateAction(
                initialValue,
                valueObservable
            )
        );

        LibraryDataProviderHelper.AddInstalledDateComponent(parentItemModel, linkedLoadoutItemsObservable);

        var matchesObservable = libraryItems
            .TransformOnObservable(libraryItem => LibraryDataProviderHelper.GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id).IsNotEmpty())
            .QueryWhenChanged(query =>
            {
                var (numInstalled, numTotal) = (0, 0);
                foreach (var isInstalled in query.Items)
                {
                    numInstalled += isInstalled ? 1 : 0;
                    numTotal++;
                }

                return new MatchesData(numInstalled, numTotal);
            });

        LibraryDataProviderHelper.AddInstallActionComponent(parentItemModel, matchesObservable, libraryItems.TransformImmutable(static x => x.AsLibraryItem()));

        return parentItemModel;
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(NexusModsLibraryItem.ReadOnly libraryItem, LibraryFilter libraryFilter)
    {
        var linkedLoadoutItemsObservable = LibraryDataProviderHelper
            .GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id)
            .RefCount();

        var fileMetadata = libraryItem.FileMetadata;

        var itemModel = new CompositeItemModel<EntityId>(libraryItem.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: fileMetadata.Name));
        itemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(value: libraryItem.GetCreatedAt()));
        itemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(value: fileMetadata.Version));

        if (libraryItem.FileMetadata.Size.TryGet(out var size))
            itemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(value: size));

        LibraryDataProviderHelper.AddInstalledDateComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddInstallActionComponent(itemModel, libraryItem.AsLibraryItem(), linkedLoadoutItemsObservable);

        // Update available
        var newestVersionObservable = _modUpdateService
            .GetNewestFileVersionObservable(fileMetadata)
            .Select(static optional => optional.Convert(static fileMetadata => fileMetadata.Version));

        itemModel.AddObservable(
            key: LibraryColumns.ItemVersion.NewVersionComponentKey,
            observable: newestVersionObservable,
            componentFactory: static (valueObservable, initialValue) => new StringComponent(
                initialValue,
                valueObservable
            )
        );

        // Update button
        var newestFile = _modUpdateService.GetNewestFileVersionObservable(fileMetadata);

        itemModel.AddObservable(
            key: LibraryColumns.Actions.UpdateComponentKey,
            observable: newestFile,
            componentFactory: static (valueObservable, initialValue) => new LibraryComponents.UpdateAction(
                initialValue,
                valueObservable
            )
        );

        return itemModel;
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
