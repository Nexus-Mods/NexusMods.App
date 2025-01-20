using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.App.UI.Pages;

public interface ILoadoutDataProvider
{
    IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems(LoadoutFilter loadoutFilter);

    IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter);
}

public class LoadoutFilter
{
    public required LoadoutId LoadoutId { get; init; }
    public required Optional<LoadoutItemGroupId> CollectionGroupId { get; init; }
}

public static class LoadoutDataProviderHelper
{
    public static ChangeSet<LoadoutItem.ReadOnly, EntityId> GetLinkedLoadoutItems(
        IDb db,
        LoadoutFilter loadoutFilter,
        LibraryItemId libraryItemId)
    {
        List<EntityId> entityIds;

        if (loadoutFilter.CollectionGroupId.HasValue)
        {
            entityIds = db.Datoms(
                (LibraryLinkedLoadoutItem.LibraryItemId, libraryItemId),
                (LoadoutItem.ParentId, loadoutFilter.CollectionGroupId.Value)
            );
        }
        else
        {
            entityIds = db.Datoms(
                (LibraryLinkedLoadoutItem.LibraryItemId, libraryItemId),
                (LoadoutItem.LoadoutId, loadoutFilter.LoadoutId)
            );
        }

        var changeSet = new ChangeSet<LoadoutItem.ReadOnly, EntityId>();

        foreach (var entityId in entityIds)
        {
            var item = LoadoutItem.Load(db, entityId);
            if (!item.IsValid()) continue;

            changeSet.Add(new Change<LoadoutItem.ReadOnly, EntityId>(ChangeReason.Add, entityId, item));
        }

        return changeSet;
    }

    public static CompositeItemModel<EntityId> ToChildItemModel(IConnection connection, LoadoutItem.ReadOnly loadoutItem)
    {
        var itemModel = new CompositeItemModel<EntityId>(loadoutItem.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: loadoutItem.Name));
        itemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(value: loadoutItem.GetCreatedAt()));

        var isEnabledObservable = LoadoutItem.Observe(connection, loadoutItem.Id).Select(static item => (bool?)!item.IsDisabled);
        itemModel.Add(LoadoutColumns.IsEnabled.ComponentKey, new LoadoutComponents.IsEnabled(
            valueComponent: new ValueComponent<bool?>(
                initialValue: !loadoutItem.IsDisabled,
                valueObservable: isEnabledObservable
            ),
            itemId: loadoutItem.LoadoutItemId
        ));

        return itemModel;
    }

    public static void AddDateComponent(
        CompositeItemModel<EntityId> parentItemModel,
        DateTimeOffset initialValue,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var dateObservable = linkedItemsObservable
            .QueryWhenChanged(query => query.Items
                .Select(static item => item.GetCreatedAt())
                .Min()
            );

        parentItemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(
            initialValue: initialValue,
            valueObservable: dateObservable
        ));
    }

    public static void AddIsEnabled(
        IConnection connection,
        CompositeItemModel<EntityId> parentItemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var isEnabledObservable = linkedItemsObservable
            .TransformOnObservable(item => LoadoutItem.Observe(connection, item.Id))
            .TransformImmutable(static item => !item.IsDisabled)
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
            });

        parentItemModel.Add(LoadoutColumns.IsEnabled.ComponentKey, new LoadoutComponents.IsEnabled(
            valueComponent: new ValueComponent<bool?>(
                initialValue: true,
                valueObservable: isEnabledObservable
            ),
            linkedItemsObservable.TransformImmutable(static item => item.LoadoutItemId)
        ));
    }

    public static IObservable<IChangeSet<Datom, EntityId>> FilterInStaticLoadout(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        LoadoutFilter loadoutFilter)
    {
        var filterByCollection = loadoutFilter.CollectionGroupId.HasValue;
        return source.Filter(datom =>
        {
            var item = LoadoutItem.Load(connection.Db, datom.E);
            if (!item.LoadoutId.Equals(loadoutFilter.LoadoutId)) return false;
            if (filterByCollection) 
                return item.IsChildOf(loadoutFilter.CollectionGroupId.Value);
            return true;
        });
    }

    public static LoadoutItemModel ToLoadoutItemModel(IConnection connection, LibraryLinkedLoadoutItem.ReadOnly libraryLinkedLoadoutItem, IServiceProvider serviceProvider, bool loadThumbnail)
    {
        // NOTE(erri120): We'll only show the library linked loadout item group for now.
        // Showing sub-groups, like SMAPI mods for Stardew Valley, will not be shown for now.
        // We'll probably have a setting or something that the game extension can control.

        return ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup(), serviceProvider, loadThumbnail);

        // var db = libraryLinkedLoadoutItem.Db;

        // NOTE(erri120): We provide the installer with a "parent" LoadoutItemGroup. The installer
        // has two options: 1) they add all files to this group, 2) they add more groups to the group.
        // The LibraryLinkedLoadoutItem should only contain all files or all groups and this merge
        // figures out what case we have.
        // Heterogeneous data where the group has files and more groups is forbidden but currently not enforced.
        // var childDatoms = db.Datoms(LoadoutItem.ParentId, libraryLinkedLoadoutItem.Id);
        // var groupDatoms = db.Datoms(LoadoutItemGroup.Group, Null.Instance);
        // var groupIds = groupDatoms.MergeByEntityId(childDatoms);
        // var onlyHasFiles = groupIds.Count == 0;
        //
        // return [ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup())];
        // if (onlyHasFiles)
        // {
        //     return [ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup())];
        // }
        //
        // var arr = GC.AllocateUninitializedArray<LoadoutItemModel>(length: groupIds.Count);
        // for (var i = 0; i < groupIds.Count; i++)
        // {
        //     arr[i] = ToLoadoutItemModel(connection, LoadoutItemGroup.Load(db, groupIds[i]));
        // }

        // return arr;
    }

    private static LoadoutItemModel ToLoadoutItemModel(IConnection connection, LoadoutItemGroup.ReadOnly loadoutItemGroup, IServiceProvider serviceProvider, bool loadThumbnail)
    {
        var observable = LoadoutItemGroup
            .Observe(connection, loadoutItemGroup.Id)
            .Replay(bufferSize: 1)
            .AutoConnect();

        var nameObservable = observable.Select(static item => item.AsLoadoutItem().Name);
        var isEnabledObservable = observable.Select<LoadoutItemGroup.ReadOnly, bool?>(static item => !item.AsLoadoutItem().IsDisabled);

        // TODO: version (need to ask the game extension)
        // TODO: size (probably with RevisionsWithChildUpdates)

        var model = new LoadoutItemModel(loadoutItemGroup.Id, serviceProvider, connection, loadThumbnail, loadThumbnail)
        {
            NameObservable = nameObservable,
            IsEnabledObservable = isEnabledObservable,
        };

        model.InstalledAt.Value = loadoutItemGroup.GetCreatedAt();
        return model;
    }
}
