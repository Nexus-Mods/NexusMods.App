using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using UIObservableExtensions = NexusMods.App.UI.Extensions.ObservableExtensions;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class ManuallyCreatedArchiveDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;

    public ManuallyCreatedArchiveDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    // TODO: update once we have game information on Local Files
    public LibraryFile.ReadOnly[] GetAllFiles(GameId gameId, IDb? db = null) => [];

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter)
    {
        return ManuallyCreatedArchive
            .ObserveAll(_connection)
            .Transform(archive => ToLibraryItemModel(libraryFilter, archive));
    }

    public IObservable<int> CountLibraryItems(LibraryFilter libraryFilter)
    {
        return ManuallyCreatedArchive
            .ObserveAll(_connection)
            .QueryWhenChanged(query => query.Count)
            .Prepend(0);
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(LibraryFilter libraryFilter, ManuallyCreatedArchive.ReadOnly archive)
    {
        var linkedLoadoutItemsObservable = LibraryDataProviderHelper
            .GetLinkedLoadoutItems(_connection, libraryFilter, archive.Id)
            .RefCount();

        var childrenObservable = UIObservableExtensions.ReturnFactory(() =>
        {
            var itemModel = new CompositeItemModel<EntityId>(archive.Id);
            SetupLibraryItemModel(itemModel, archive, linkedLoadoutItemsObservable);

            return new ChangeSet<CompositeItemModel<EntityId>, EntityId>([
                new Change<CompositeItemModel<EntityId>, EntityId>(
                    reason: ChangeReason.Add,
                    key: archive.Id,
                    current: itemModel
                )]
            );
        });
        
        var hasChildrenObservable = childrenObservable.IsNotEmpty();

        var parentItemModel = new CompositeItemModel<EntityId>(archive.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        SetupLibraryItemModel(parentItemModel, archive, linkedLoadoutItemsObservable);

        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));
        return parentItemModel;
    }

    private static void SetupLibraryItemModel(
        CompositeItemModel<EntityId> itemModel,
        ManuallyCreatedArchive.ReadOnly archive,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedLoadoutItemsObservable)
    {
        itemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: archive.AsLibraryArchive().AsLibraryFile().AsLibraryItem().Name));
        itemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(value: archive.GetCreatedAt()));
        itemModel.Add(LibraryColumns.Actions.LibraryItemIdsComponentKey, new LibraryComponents.LibraryItemIds(archive.AsLibraryArchive().AsLibraryFile().AsLibraryItem()));

        LibraryDataProviderHelper.AddInstalledDateComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddInstallActionComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddViewChangelogActionComponent(itemModel, isEnabled: false);
        LibraryDataProviderHelper.AddViewModPageActionComponent(itemModel, isEnabled: false);
        LibraryDataProviderHelper.AddHideUpdatesActionComponent(itemModel, isEnabled: false, isVisible: false);
    }

    private IObservable<IChangeSet<ManuallyCreatedArchive.ReadOnly, EntityId>> FilterLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return ManuallyCreatedArchive
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
        return FilterLoadoutItems(loadoutFilter).Transform(archive => ToLoadoutItemModel(loadoutFilter, archive));
    }

    public IObservable<int> CountLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).QueryWhenChanged(static query => query.Count).Prepend(0);
    }

    private CompositeItemModel<EntityId> ToLoadoutItemModel(LoadoutFilter loadoutFilter, ManuallyCreatedArchive.ReadOnly archive)
    {
        var linkedItemsObservable = _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItem, archive)
            .AsEntityIds()
            .FilterInStaticLoadout(_connection, loadoutFilter)
            .Transform(datom => LoadoutItem.Load(_connection.Db, datom.E))
            .RefCount();

        var hasChildrenObservable = linkedItemsObservable.IsNotEmpty();
        var childrenObservable = linkedItemsObservable.Transform(loadoutItem => LoadoutDataProviderHelper.ToChildItemModel(_connection, loadoutItem));

        var parentItemModel = new CompositeItemModel<EntityId>(archive.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: archive.AsLibraryArchive().AsLibraryFile().AsLibraryItem().Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));

        LoadoutDataProviderHelper.AddDateComponent(parentItemModel, archive.GetCreatedAt(), linkedItemsObservable);
        LoadoutDataProviderHelper.AddCollections(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddParentCollectionsDisabled(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddMixLockedAndParentDisabled(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddLockedEnabledStates(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddEnabledStateToggle(_connection, parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddLoadoutItemIds(parentItemModel, linkedItemsObservable);
        LoadoutDataProviderHelper.AddUninstallItemComponent(parentItemModel, linkedItemsObservable);
        

        return parentItemModel;
    }
}
