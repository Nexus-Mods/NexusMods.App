using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Resources;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
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

public class CollectionDataProvider
{
    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;
    private readonly CollectionDownloader _collectionDownloader;
    private readonly IResourceLoader<EntityId, Bitmap> _thumbnailLoader;

    public CollectionDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _collectionDownloader = serviceProvider.GetRequiredService<CollectionDownloader>();
        _thumbnailLoader = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveCollectionItems(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        IObservable<CollectionDownloadsFilter> filterObservable,
        LoadoutId loadoutId)
    {
        var collectionGroupObservable = _collectionDownloader
            .GetCollectionGroupObservable(revisionMetadata, loadoutId)
            .Replay(bufferSize: 1)
            .RefCount();

        return _connection
            .ObserveDatoms(CollectionDownloadEntity.CollectionRevision, revisionMetadata)
            .AsEntityIds()
            .Transform(datom => CollectionDownloadEntity.Load(_connection.Db, datom.E))
            .FilterOnObservable(downloadEntity => ShouldShow(downloadEntity, filterObservable))
            .Transform(downloadEntity =>
            {
                if (downloadEntity.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
                {
                    return ToItemModel(nexusModsDownload, collectionGroupObservable);
                }

                if (downloadEntity.TryGetAsCollectionDownloadExternal(out var externalDownload))
                {
                    return ToItemModel(externalDownload, collectionGroupObservable);
                }

                if (downloadEntity.TryGetAsCollectionDownloadBundled(out var bundledDownload))
                {
                    return ToItemModel(bundledDownload, collectionGroupObservable);
                }

                throw new UnreachableException();
            });
    }

    private static IObservable<bool> ShouldShow(CollectionDownloadEntity.ReadOnly downloadEntity, IObservable<CollectionDownloadsFilter> filterObservable)
    {
        return filterObservable.Select(filter =>
        {
            if (!downloadEntity.IsCollectionDownloadNexusMods() && !downloadEntity.IsCollectionDownloadExternal() && !downloadEntity.IsCollectionDownloadBundled()) return false;

            return filter switch
            {
                CollectionDownloadsFilter.OnlyRequired => !downloadEntity.IsOptional,
                CollectionDownloadsFilter.OnlyOptional => downloadEntity.IsOptional,
            };
        });
    }

    private CompositeItemModel<EntityId> ToItemModel(
        CollectionDownloadNexusMods.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        var itemModel = new CompositeItemModel<EntityId>(download.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: download.FileMetadata.Name));
        itemModel.Add(SharedColumns.Name.ImageComponentKey, ImageComponent.FromPipeline(_thumbnailLoader, download.FileMetadata.ModPageId, initialValue: ImagePipelines.ModPageThumbnailFallback));
        itemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(value: download.FileMetadata.Version));

        if (download.FileMetadata.Size.TryGet(out var size))
            itemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(value: size));

        var statusObservable = _collectionDownloader.GetStatusObservable(download.AsCollectionDownload(), groupObservable).ToObservable();
        var downloadJobStatusObservable = GetJobStatusObservable<NexusModsDownloadJob>(job => job.FileMetadata.Id == download.FileMetadata);

        AddDownloadAction(
            itemModel: itemModel,
            downloadEntity: download.AsCollectionDownload(),
            statusObservable: statusObservable,
            groupObservable: groupObservable.ToObservable(),
            key: CollectionColumns.Actions.NexusModsDownloadComponentKey,
            componentFactory: () => new CollectionComponents.NexusModsDownloadAction(
                downloadEntity: download,
                downloadJobStatusObservable: downloadJobStatusObservable,
                isDownloadedObservable: statusObservable.Select(status => status.IsDownloaded())
            )
        );

        AddInstallAction(itemModel, download.AsCollectionDownload(), statusObservable, groupObservable.ToObservable());

        return itemModel;
    }

    private CompositeItemModel<EntityId> ToItemModel(
        CollectionDownloadExternal.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        var itemModel = new CompositeItemModel<EntityId>(download.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: download.AsCollectionDownload().Name));
        itemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));
        itemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(value: download.Md5.ToString()));
        itemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(value: download.Size));

        var statusObservable = _collectionDownloader.GetStatusObservable(download.AsCollectionDownload(), groupObservable).ToObservable();
        var downloadJobStatusObservable = GetJobStatusObservable<ExternalDownloadJob>(job => job.ExpectedMd5 == download.Md5);

        AddDownloadAction(
            itemModel: itemModel,
            downloadEntity: download.AsCollectionDownload(),
            statusObservable: statusObservable,
            groupObservable: groupObservable.ToObservable(),
            key: CollectionColumns.Actions.ExternalDownloadComponentKey,
            componentFactory: () => new CollectionComponents.ExternalDownloadAction(
                downloadEntity: download,
                downloadJobStatusObservable: downloadJobStatusObservable,
                isDownloadedObservable: statusObservable.Select(status => status.IsDownloaded())
            )
        );

        AddInstallAction(itemModel, download.AsCollectionDownload(), statusObservable, groupObservable.ToObservable());

        return itemModel;
    }

    private CompositeItemModel<EntityId> ToItemModel(
        CollectionDownloadBundled.ReadOnly download,
        IObservable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        var itemModel = new CompositeItemModel<EntityId>(download.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: download.AsCollectionDownload().Name));
        itemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));

        var statusObservable = _collectionDownloader.GetStatusObservable(download.AsCollectionDownload(), groupObservable).ToObservable();
        AddInstallAction(itemModel, download.AsCollectionDownload(), statusObservable, groupObservable.ToObservable());

        return itemModel;
    }

    private static Observable<bool> ShouldAddObservable(
        CollectionDownloadEntity.ReadOnly downloadEntity,
        Observable<CollectionDownloadStatus> statusObservable,
        Observable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        return statusObservable.CombineLatest(groupObservable, static (a, b) => (a, b)).Select(downloadEntity, static (tuple, downloadEntity) =>
        {
            var (status, collectionGroup) = tuple;
            var (isDownloaded, isInstalled, isOptional, hasGroup) = (status.IsDownloaded(), status.IsInstalled(out _), downloadEntity.IsOptional, collectionGroup.HasValue);
            return (isDownloaded, isInstalled, isOptional, hasGroup) switch
            {
                (isDownloaded: false, _, _, _) => false,
                (_, isInstalled: true, _, _) => true,
                (_, isInstalled: false, isOptional: true, hasGroup: true) => true,
                _ => false,
            };
        });
    }

    private static void AddInstallAction(
        CompositeItemModel<EntityId> itemModel,
        CollectionDownloadEntity.ReadOnly downloadEntity,
        Observable<CollectionDownloadStatus> statusObservable,
        Observable<Optional<CollectionGroup.ReadOnly>> groupObservable)
    {
        itemModel.AddObservable(
            key: CollectionColumns.Actions.InstallComponentKey,
            shouldAddObservable: ShouldAddObservable(downloadEntity, statusObservable, groupObservable),
            componentFactory: () => new CollectionComponents.InstallAction(
                downloadEntity: downloadEntity,
                isInstalledObservable: statusObservable.Select(status => status.IsInstalled(out _))
            )
        );
    }

    private static void AddDownloadAction<TComponent>(
        CompositeItemModel<EntityId> itemModel,
        CollectionDownloadEntity.ReadOnly downloadEntity,
        Observable<CollectionDownloadStatus> statusObservable,
        Observable<Optional<CollectionGroup.ReadOnly>> groupObservable,
        ComponentKey key,
        Func<TComponent> componentFactory)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        itemModel.AddObservable(
            key: key,
            shouldAddObservable: ShouldAddObservable(downloadEntity, statusObservable, groupObservable).Select(static b => !b),
            componentFactory: componentFactory
        );
    }

    private Observable<JobStatus> GetJobStatusObservable<TJobDefinition>(Func<TJobDefinition, bool> predicate)
        where TJobDefinition : IJobDefinition
    {
        return _jobMonitor
            .GetObservableChangeSet<TJobDefinition>()
            .FilterImmutable(job =>
            {
                if (job.Definition is not TJobDefinition definition) return false;
                return predicate(definition);
            })
            .QueryWhenChanged(static query => query.Items.OptionalMaxBy(static job => job.Status, JobStatusComparer.Instance))
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value)
            .SelectMany(static job => job.ObservableStatus)
            .ToObservable()
            .Prepend(() => JobStatus.None)
            .ObserveOnUIThreadDispatcher();
    }
}
