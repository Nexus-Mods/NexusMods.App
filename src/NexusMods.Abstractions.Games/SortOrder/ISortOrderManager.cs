using System.Diagnostics.Contracts;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Games;


/// <summary>
/// Represents the single central manager for load order related updates.
/// One instance per game.
/// </summary>
public interface ILoadOrderManager
{
    internal ValueTask<IDisposable> Lock(CancellationToken token = default);
    
    public ValueTask UpdateLoadOrders(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId = default, CancellationToken token = default);
    
    public ISortOrderVariety[] GetSortOrderVarieties();
}


/// <summary>
/// Represents a specific variety of sort order for a specific game.
/// Handles updating SortOrder entities of this variety.
/// One instance for each variety per game.
/// </summary>
/// <examples>
/// Cyberpunk RedMod load order;
/// Cyberpunk Archive load order;
/// Skyrim SE plugin load order;
/// </examples>
public interface ISortOrderVariety
{
    [Pure]
    public SortOrderId GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity);
    
    [Pure]
    public IObservable<IChangeSet<ISortableItem, ISortItemKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId);
    
    [Pure]
    public IReadOnlyList<ISortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db = null);
    
    [Pure]
    public ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<ISortableItem> items, IDb? db = null, CancellationToken token = default);
    
    [Pure]
    public ValueTask MoveItems(SortOrderId sortOrderId, ISortItemKey[] itemsToMove, ISortItemKey dropTargetItem, TargetRelativePosition relativePosition, IDb? db = null, CancellationToken token = default);

    [Pure]
    public ValueTask MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db = null, CancellationToken token = default);
    
    /// <summary>
    /// Reconcile the SortOrder with the latest data from the Db, adding or removing items as necessary.
    /// </summary>
    [Pure]
    public ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default);
}
