using System.ComponentModel;
using System.Diagnostics.Contracts;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents a specific variety of sort order for a specific game.
/// Handles updating all the SortOrder entities of this variety.
/// One instance for each variety per game.
/// </summary>
/// <examples>
/// Cyberpunk RedMod load order;
/// Cyberpunk Archive load order;
/// Skyrim SE plugin load order;
/// </examples>
public interface ISortOrderVariety
{
    /// <summary>
    /// Returns an id identifying the variety of the sort order.
    /// </summary>
    SortOrderVarietyId SortOrderVarietyId { get; }
    
    /// <summary>
    /// Default direction (ascending/descending) in which sortIndexes should be sorted and displayed
    /// </summary>
    /// <remarks>
    /// Usually ascending, but could be different depending on what the community prefers and is used to
    /// </remarks>
    ListSortDirection SortDirectionDefault { get; }
    
    /// <summary>
    /// Defines whether smaller or greater index numbers win in case of conflicts between items in sorting order
    /// </summary>
    IndexOverrideBehavior IndexOverrideBehavior { get; }
    
    /// <summary>
    /// Contains UI strings and metadata for the sort order type
    /// </summary>
    SortOrderUiMetadata SortOrderUiMetadata { get; }
    
    /// <summary>
    /// Returns the SortOrderId for this variety for the given parent entity.
    /// </summary>
    [Pure]
    public SortOrderId GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity);
    
    /// <summary>
    /// Returns an observable change set of ISortableItems for the given SortOrderId.
    /// </summary>
    [Pure]
    public IObservable<IChangeSet<ISortableItem, ISortItemKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId);
    
    /// <summary>
    /// Returns a list of ISortableItems for the given SortOrderId.
    /// The latest database revision is used unless a specific IDb is provided.
    /// </summary>
    [Pure]
    public IReadOnlyList<ISortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db = null);
    
    /// <summary>
    /// Sets the sort order to match the one of the passed keys.
    /// </summary>
    [Pure]
    public ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<ISortItemKey> items, IDb? db = null, CancellationToken token = default);
    
    /// <summary>
    /// Moves the given items to be before or after the target item in ascending index sort order.
    /// The relative index order of the moved items is preserved.
    /// Validity and outcome of the move may depend on game-specific logic, so only some or none of the items may be moved.
    /// </summary>
    [Pure]
    public ValueTask MoveItems(SortOrderId sortOrderId, ISortItemKey[] itemsToMove, ISortItemKey dropTargetItem, TargetRelativePosition relativePosition, IDb? db = null, CancellationToken token = default);

    /// <summary>
    /// Sets the relative position of a sortable item in the sort order
    /// </summary>
    /// <param name="sourceItem">Key of the item to move</param>
    /// <param name="delta">positive or negative index delta</param>
    [Pure]
    public ValueTask MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db = null, CancellationToken token = default);
    
    /// <summary>
    /// Reconcile the SortOrder with the latest data from the Db, adding or removing items as necessary.
    /// </summary>
    [Pure]
    public ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default);
}

