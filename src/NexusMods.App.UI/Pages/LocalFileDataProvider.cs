using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.NexusModsApi;
using UIObservableExtensions = NexusMods.App.UI.Extensions.ObservableExtensions;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class LocalFileDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;

    public LocalFileDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    // TODO: update once we have game information on Local Files
    public LibraryFile.ReadOnly[] GetAllFiles(GameId gameId, IDb? db = null) => [];

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter)
    {
        return LocalFile
            .ObserveAll(_connection)
            .Transform(localFile => ToLibraryItemModel(libraryFilter, localFile));
    }

    public IObservable<int> CountLibraryItems(LibraryFilter libraryFilter)
    {
        return LocalFile
            .ObserveAll(_connection)
            .QueryWhenChanged(query => query.Count)
            .Prepend(0);
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(LibraryFilter libraryFilter, LocalFile.ReadOnly localFile)
    {
        var linkedLoadoutItemsObservable = LibraryDataProviderHelper
            .GetLinkedLoadoutItems(_connection, libraryFilter, localFile.Id)
            .RefCount();

        var childrenObservable = UIObservableExtensions.ReturnFactory(() =>
        {
            var itemModel = new CompositeItemModel<EntityId>(localFile.Id);
            SetupLibraryItemModel(itemModel, localFile, linkedLoadoutItemsObservable);

            return new ChangeSet<CompositeItemModel<EntityId>, EntityId>([
                new Change<CompositeItemModel<EntityId>, EntityId>(
                    reason: ChangeReason.Add,
                    key: localFile.Id,
                    current: itemModel
                )]
            );
        });
        
        var hasChildrenObservable = childrenObservable.IsNotEmpty();

        var parentItemModel = new CompositeItemModel<EntityId>(localFile.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        SetupLibraryItemModel(parentItemModel, localFile, linkedLoadoutItemsObservable);

        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));
        return parentItemModel;
    }

    private static void SetupLibraryItemModel(
        CompositeItemModel<EntityId> itemModel,
        LocalFile.ReadOnly localFile,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedLoadoutItemsObservable)
    {
        itemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: localFile.AsLibraryFile().AsLibraryItem().Name));
        itemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(value: localFile.GetCreatedAt()));
        itemModel.Add(SharedColumns.ItemSize.ComponentKey, new SizeComponent(value: localFile.AsLibraryFile().Size));
        itemModel.Add(LibraryColumns.Actions.LibraryItemIdsComponentKey, new LibraryComponents.LibraryItemIds(localFile.AsLibraryFile().AsLibraryItem()));

        LibraryDataProviderHelper.AddInstalledDateComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddInstallActionComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddViewChangelogActionComponent(itemModel, isEnabled: false);
        LibraryDataProviderHelper.AddViewModPageActionComponent(itemModel, isEnabled: false);
        LibraryDataProviderHelper.AddHideUpdatesActionComponent(itemModel, isEnabled: false, isVisible: false);
        
        // Get related collections
        var md5 = localFile.AsLibraryFile().Md5;
        if (!md5.HasValue)
        {
            LibraryDataProviderHelper.AddRelatedCollectionsComponent(itemModel, linkedLoadoutItemsObservable);
        }
        else
        {
            var conn = localFile.Db.Connection;

            var relatedDownloadedCollectionsObservable = conn
                .ObserveDatoms(CollectionDownloadExternal.Md5)
                .FilterImmutable(datom => Md5Value.From(UInt128Serializer.Read(datom.ValueSpan)) == md5.Value)
                .AsEntityIds()
                .Distinct()
                .Transform(datom => NexusMods.Abstractions.NexusModsLibrary.Models.CollectionDownload.Load(conn.Db, datom.E).CollectionRevision.Collection)
                .ChangeKey(collection => collection.Id);
            
            LibraryDataProviderHelper.AddRelatedCollectionsComponent(itemModel, linkedLoadoutItemsObservable, relatedDownloadedCollectionsObservable );
        }
        
    }

    private IObservable<IChangeSet<LocalFile.ReadOnly, EntityId>> FilterLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return LocalFile
            .ObserveAll(_connection)
            .FilterOnObservable((_, entityId) => _connection
                .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                .AsEntityIds()
                .FilterInStaticLoadout(_connection, loadoutFilter)
                .IsNotEmpty()
            );
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).Transform(localFile => ToLoadoutItemModel(loadoutFilter, localFile));
    }

    public IObservable<int> CountLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).QueryWhenChanged(static query => query.Count).Prepend(0);
    }

    private CompositeItemModel<EntityId> ToLoadoutItemModel(LoadoutFilter loadoutFilter, LocalFile.ReadOnly localFile)
    {
        var linkedItemsObservable = _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItem, localFile)
            .AsEntityIds()
            .FilterInStaticLoadout(_connection, loadoutFilter)
            .Transform(datom => LoadoutItem.Load(_connection.Db, datom.E))
            .RefCount();

        var hasChildrenObservable = linkedItemsObservable.IsNotEmpty();
        var childrenObservable = linkedItemsObservable.Transform(loadoutItem => ToLocalChildLoadoutItemModel(_connection, loadoutItem));

        var parentItemModel = new CompositeItemModel<EntityId>(localFile.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: localFile.AsLibraryFile().AsLibraryItem().Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));

        LoadoutDataProviderHelper.AddDateComponent(parentItemModel, localFile.GetCreatedAt(), linkedItemsObservable);
        LoadoutDataProviderHelper.AddCollections(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddParentCollectionsDisabled(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddMixLockedAndParentDisabled(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddLockedEnabledStates(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddEnabledStateToggle(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddLoadoutItemIds(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddViewModPageActionComponent(parentItemModel, isEnabled: false);
        LoadoutDataProviderHelper.AddViewModFilesActionComponent(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddUninstallItemComponent(parentItemModel, linkedItemsObservable);

        return parentItemModel;
    }
    
    private static CompositeItemModel<EntityId> ToLocalChildLoadoutItemModel(IConnection connection, LoadoutItem.ReadOnly loadoutItem)
    {
        var childModel = LoadoutDataProviderHelper.ToChildItemModel(connection, loadoutItem);
        LoadoutDataProviderHelper.AddViewModPageActionComponent(childModel, isEnabled: false);
        return childModel;
    }
}
