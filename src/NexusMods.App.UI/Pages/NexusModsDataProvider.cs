using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
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
using NexusMods.App.UI.Pages.LoadoutPage;
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

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _serviceProvider = serviceProvider;
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveCollectionItems(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        IObservable<CollectionDownloadsFilter> filterObservable)
    {
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
                    return ToLibraryItemModelObservable(revisionMetadata, nexusModsDownload);
                }

                if (downloadEntity.TryGetAsCollectionDownloadExternal(out var externalDownload))
                {
                    return System.Reactive.Linq.Observable.Return(ToLibraryItemModel(externalDownload));
                }

                throw new UnreachableException();
            });
    }

    private IObservable<ILibraryItemModel> ToLibraryItemModelObservable(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        CollectionDownloadNexusMods.ReadOnly nexusModsDownload)
    {
        var isInLibraryObservable = CollectionDownloader
            .IsDownloadedObservable(_connection, nexusModsDownload)
            .Prepend(CollectionDownloader.IsDownloaded(nexusModsDownload, nexusModsDownload.Db));

        // NOTE(erri120): different behavior depending on whether the download is optional or not.
        // Optional downloads can be individually installed by the user, Required downloads should only
        // show that they are installed, but not offer an install button.
        var observable = nexusModsDownload.AsCollectionDownload().IsOptional ? isInLibraryObservable : isInLibraryObservable.SelectMany(IObservable<bool> (isInLibrary) =>
        {
            if (!isInLibrary) return System.Reactive.Linq.Observable.Return(false);
            if (!CollectionDownloader.TryGetDownloadedItem(nexusModsDownload, _connection.Db, out var libraryItem)) throw new NotImplementedException();
            return GetIsInstalledObservable(libraryItem.AsLibraryItem(), revisionMetadata);
        }).DistinctUntilChanged();

        return observable.Select(ILibraryItemModel (isInLibrary) =>
        {
            if (!isInLibrary)
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
                    IsInLibraryObservable = isInLibraryObservable.ToObservable(),
                    DownloadJobObservable = downloadJobObservable,
                };

                model.Name.Value = nexusModsDownload.FileMetadata.Name;
                model.Version.Value = nexusModsDownload.FileMetadata.Version;
                if (nexusModsDownload.FileMetadata.Size.TryGet(out var size))
                    model.ItemSize.Value = size;

                return model;
            }
            else
            {
                if (!CollectionDownloader.TryGetDownloadedItem(nexusModsDownload, _connection.Db, out var libraryItem))
                {
                    throw new NotImplementedException();
                }

                var isInstalledObservable = GetIsInstalledObservable(libraryItem.AsLibraryItem(), revisionMetadata).ToObservable();
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
        });
    }

    private IObservable<bool> GetIsInstalledObservable(LibraryItem.ReadOnly libraryItem, CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        var isInstalledObservable = _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryItem.Id)
            .TransformImmutable(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E))
            .TransformImmutable(item =>
            {
                if (!LoadoutItem.Parent.TryGetValue(item, out var parentId)) return default(NexusCollectionLoadoutGroup.ReadOnly);
                return NexusCollectionLoadoutGroup.Load(item.Db, parentId);
            })
            .FilterImmutable(static parent => parent.IsValid())
            .FilterImmutable(parent => parent.RevisionId == revisionMetadata)
            .IsNotEmpty();

        return isInstalledObservable;
    }

    private ILibraryItemModel ToLibraryItemModel(CollectionDownloadExternal.ReadOnly externalDownload)
    {
        var isInLibraryObservable = CollectionDownloader.IsDownloadedObservable(_connection, externalDownload)
            .ToObservable()
            .Prepend(externalDownload, static download => CollectionDownloader.IsDownloaded(download, download.Db));

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

    private ILibraryItemModel ToLibraryItemModel(NexusModsModPageMetadata.ReadOnly modPageMetadata, LibraryFilter libraryFilter)
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

    public IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems(LoadoutFilter loadoutFilter)
    {
        // NOTE(erri120): For the nested loadout view, we create "fake" models for
        // the mod pages as parents. Each child will be a LibraryLinkedLoadoutItem
        // that links to a NexusModsLibraryFile that links to the mod page.
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
            .TransformAsync(async modPage =>
            {
                // TODO: dispose
                var cache = new SourceCache<Datom, EntityId>(static datom => datom.E);
                var disposable = _connection
                    .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPage.Id).AsEntityIds()
                    .FilterOnObservable((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).IsNotEmpty())
                    // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
                    .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .Adapt(new SourceCacheAdapter<Datom, EntityId>(cache))
                    .SubscribeWithErrorLogging();

                var hasChildrenObservable = cache.Connect().IsNotEmpty();
                var childrenObservable = cache.Connect().Transform(libraryLinkedLoadoutItemDatom =>
                {
                    var libraryLinkedLoadoutItem = LibraryLinkedLoadoutItem.Load(_connection.Db, libraryLinkedLoadoutItemDatom.E);
                    return LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem, _serviceProvider, false);
                });

                var installedAtObservable = cache.Connect()
                    .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e).GetCreatedAt())
                    .QueryWhenChanged(query =>
                    {
                        if (query.Count == 0) return DateTimeOffset.MinValue;
                        return query.Items.Max();
                    });

                var loadoutItemIdsObservable = cache.Connect().Transform((_, e) => (LoadoutItemId) e);

                var isEnabledObservable = cache.Connect()
                    .TransformOnObservable(datom => LoadoutItem.Observe(_connection, datom.E).Select(item => !item.IsDisabled))
                    .QueryWhenChanged(query =>
                    {
                        var isEnabled = Optional<bool>.None;
                        foreach (var isItemEnabled in query.Items)
                        {
                            if (!isEnabled.HasValue)
                            {
                                isEnabled = isItemEnabled;
                            }
                            else
                            {
                                if (isEnabled.Value != isItemEnabled) return (bool?)null;
                            }
                        }

                        return isEnabled.HasValue ? isEnabled.Value : null;
                    }).DistinctUntilChanged(x => x is null ? -1 : x.Value ? 1 : 0);

                var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(_serviceProvider);
                Bitmap? bitmap = null;
                try { bitmap = (await modPageThumbnailPipeline.LoadResourceAsync(modPage.Id, CancellationToken.None)).Data; }
                catch (Exception) { /* Ignore missing thumbnail errors, in case user is e.g. migrating from older version */ }
                
                LoadoutItemModel model = new FakeParentLoadoutItemModel(loadoutItemIdsObservable, _serviceProvider, hasChildrenObservable, childrenObservable, bitmap)
                {
                    NameObservable = System.Reactive.Linq.Observable.Return(modPage.Name),
                    InstalledAtObservable = installedAtObservable,
                    IsEnabledObservable = isEnabledObservable,
                };

                return model;
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
