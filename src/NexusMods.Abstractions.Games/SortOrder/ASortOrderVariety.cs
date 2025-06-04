using System.ComponentModel;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Abstract base class for a variety of sort order for a specific game.
/// </summary>
public abstract class ASortOrderVariety<TItem, TKey> : ISortOrderVariety<TItem, TKey>
    where TItem : ISortableItem<TItem, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    
    #region public members
    
    /// <inheritdoc />
    public abstract SortOrderVarietyId SortOrderVarietyId { get; }

    /// <inheritdoc />
    public virtual SortOrderUiMetadata SortOrderUiMetadata => ISortOrderVariety.StaticSortOrderUiMetadata;
    
    /// <inheritdoc />
    public virtual ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    /// <inheritdoc />
    public virtual IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.GreaterIndexWins;

    /// <inheritdoc />
    public SortOrderId GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity)
    {
        throw new NotImplementedException();
        // TODO: Implement query to get SortOrder that has matching parent entity and matching SortOrderVarietyId
    }

    /// <inheritdoc />
    public abstract IObservable<IChangeSet<TItem, TKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId);

    /// <inheritdoc />
    public abstract IReadOnlyList<TItem> GetSortableItems(SortOrderId sortOrderId, IDb? db);

    /// <inheritdoc />
    public abstract ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<TKey> items, IDb? db = null, CancellationToken token = default);

    /// <inheritdoc />
    public ValueTask MoveItems(
        SortOrderId sortOrderId,
        TKey[] itemsToMove,
        TKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db = null,
        CancellationToken token = default)
    {
        throw new NotImplementedException();
        GetSortableItems(sortOrderId, db);
        
    }
    
    /// <inheritdoc />
    public abstract ValueTask MoveItemDelta(SortOrderId sortOrderId, TKey sourceItem, int delta, IDb? db = null, CancellationToken token = default);

    /// <inheritdoc />
    public abstract ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default);
    
    #endregion public members
    
    #region protected members
    
    
    
    #endregion protected members
}
