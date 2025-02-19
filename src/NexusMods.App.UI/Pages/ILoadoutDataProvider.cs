using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using R3;

namespace NexusMods.App.UI.Pages;

public interface ILoadoutDataProvider
{
    IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter);

    IObservable<int> CountLoadoutItems(LoadoutFilter loadoutFilter);
}

public class LoadoutFilter
{
    public required LoadoutId LoadoutId { get; init; }
    public required Optional<LoadoutItemGroupId> CollectionGroupId { get; init; }
}

public static class LoadoutDataProviderHelper
{
    public static IObservable<int> CountAllLoadoutItems(IServiceProvider serviceProvider, LoadoutId loadoutId)
    {
        return CountAllLoadoutItems(serviceProvider, new LoadoutFilter
        {
            LoadoutId = loadoutId,
            CollectionGroupId = Optional<LoadoutItemGroupId>.None,
        });
    }

    public static IObservable<int> CountAllLoadoutItems(IServiceProvider serviceProvider, LoadoutFilter loadoutFilter)
    {
        var loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>();
        return loadoutDataProviders
            .Select(provider => provider.CountLoadoutItems(loadoutFilter))
            .CombineLatest(static counts => counts.Sum());
    }

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

        AddCollection(connection, itemModel, loadoutItem);
        AddIsEnabled(connection, itemModel, loadoutItem);

        return itemModel;
    }

    public static void AddCollection(IConnection connection, CompositeItemModel<EntityId> itemModel, LoadoutItem.ReadOnly loadoutItem)
    {
        if (!loadoutItem.Parent.TryGetAsCollectionGroup(out var collectionGroup)) return;

        itemModel.Add(LoadoutColumns.Collections.ComponentKey, new StringComponent(value: collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().Name));
        var isParentCollectionDisabledObservable = LoadoutItem.Observe(connection, collectionGroup.Id).Select(static item => item.IsDisabled).ToObservable();

        itemModel.AddObservable(
            key: LoadoutColumns.IsEnabled.ParentCollectionDisabledComponentKey,
            shouldAddObservable: isParentCollectionDisabledObservable,
            componentFactory: () => new LoadoutComponents.ParentCollectionDisabled()
        );
    }

    public static void AddIsEnabled(IConnection connection, CompositeItemModel<EntityId> itemModel, LoadoutItem.ReadOnly loadoutItem)
    {
        var isEnabledObservable = LoadoutItem.Observe(connection, loadoutItem.Id).Select(static item => (bool?)!item.IsDisabled);
        itemModel.Add(LoadoutColumns.IsEnabled.IsEnabledComponentKey, new LoadoutComponents.IsEnabled(
            valueComponent: new ValueComponent<bool?>(
                initialValue: !loadoutItem.IsDisabled,
                valueObservable: isEnabledObservable
            ),
            itemId: loadoutItem.LoadoutItemId,
            isLocked: IsLocked(loadoutItem)
        ));
    }

    public static void AddDateComponent(
        CompositeItemModel<EntityId> parentItemModel,
        DateTimeOffset initialValue,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var dateObservable = linkedItemsObservable
            .QueryWhenChanged(query => query.Items
                .Select(static item => item.GetCreatedAt())
                .OptionalMinBy(item => item)
                .ValueOr(DateTimeOffset.MinValue)
            );

        parentItemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(
            initialValue: initialValue,
            valueObservable: dateObservable
        ));
    }

    public static void AddCollections(
        CompositeItemModel<EntityId> parentItemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var collectionsObservable = linkedItemsObservable
            .QueryWhenChanged(query => query.Items
                .Where(static item => item.Parent.IsCollectionGroup())
                .GroupBy(static item => item.ParentId)
                .Select(static grouping =>
                {
                    var optional = grouping.FirstOrOptional(static _ => true);
                    return optional.Convert(static item => item.Parent.AsLoadoutItem().Name);
                })
                .Where(static optional => optional.HasValue)
                .Select(static optional => optional.Value)
                .Order(StringComparer.OrdinalIgnoreCase)
                .SafeAggregate(defaultValue: string.Empty, static (a, b) => $"{a}, {b}")
            );

        parentItemModel.Add(LoadoutColumns.Collections.ComponentKey, new StringComponent(
            initialValue: string.Empty,
            valueObservable: collectionsObservable
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

        var isLockedObservable = linkedItemsObservable
            .TransformImmutable(static item => IsLocked(item))
            .QueryWhenChanged(static query => query.Items.Any(isLocked => isLocked))
            .ToObservable();

        parentItemModel.Add(LoadoutColumns.IsEnabled.IsEnabledComponentKey, new LoadoutComponents.IsEnabled(
            valueComponent: new ValueComponent<bool?>(
                initialValue: true,
                valueObservable: isEnabledObservable
            ),
            childrenItemIdsObservable: linkedItemsObservable.TransformImmutable(static item => item.LoadoutItemId),
            isLockedComponent: new ValueComponent<bool>(
                initialValue: false,
                valueObservable: isLockedObservable
            )
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

    public static bool IsLocked<T>(T entity) where T : struct, IReadOnlyModel<T>
    {
        return NexusCollectionItemLoadoutGroup.IsRequired.GetOptional(entity).ValueOr(false);
    }
}
