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
}
