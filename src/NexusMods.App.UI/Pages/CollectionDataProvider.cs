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
        _collectionDownloader = new CollectionDownloader(serviceProvider);
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

                throw new UnreachableException();
            });
    }

    private static IObservable<bool> ShouldShow(CollectionDownloadEntity.ReadOnly downloadEntity, IObservable<CollectionDownloadsFilter> filterObservable)
    {
        return filterObservable.Select(filter =>
        {
            if (!downloadEntity.IsCollectionDownloadNexusMods() && !downloadEntity.IsCollectionDownloadExternal()) return false;

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

        var statusObservable = _collectionDownloader.GetStatusObservable(download.AsCollectionDownload(), groupObservable);

        var downloadJobStatusObservable = _jobMonitor
            .GetObservableChangeSet<NexusModsDownloadJob>()
            .FilterImmutable(job =>
            {
                if (job.Definition is not NexusModsDownloadJob definition) return false;
                return definition.FileMetadata.Id == download.FileMetadata;
            })
            .QueryWhenChanged(static query => query.Items.OptionalMaxBy(static job => job.Status, JobStatusComparer.Instance))
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value)
            .SelectMany(static job => job.ObservableStatus)
            .ToObservable()
            .ObserveOnUIThreadDispatcher();

        itemModel.AddObservable(
            key: CollectionColumns.Actions.NexusModsDownloadComponentKey,
            shouldAddObservable: statusObservable.Select(status => status.IsNotDownloaded()).ToObservable(),
            componentFactory: () => new CollectionComponents.NexusModsDownloadAction(
                downloadEntity: download,
                downloadJobStatusObservable: downloadJobStatusObservable
            )
        );

        itemModel.AddObservable(
            key: CollectionColumns.Actions.NexusModsDownloadComponentKey,
            shouldAddObservable: statusObservable.Select(status => status.IsDownloaded()).ToObservable(),
            componentFactory: () => new CollectionComponents.InstallAction()
        );

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

        return itemModel;
    }
}
