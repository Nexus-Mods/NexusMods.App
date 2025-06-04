using System.ComponentModel;
using System.Diagnostics.Contracts;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly IConnection _connection;
    private readonly ILogger _logger;
    private readonly ISortOrderManager _manager;

    protected ASortOrderVariety(IServiceProvider serviceProvider, ISortOrderManager manager)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _logger = serviceProvider.GetRequiredService<ILogger>();
        _manager = manager;
    }
        
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
    public async ValueTask MoveItems(
        SortOrderId sortOrderId,
        TKey[] itemsToMove,
        TKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db = null,
        CancellationToken token = default)
    {
        // acquire the lock
        using var _ = await _manager.Lock(token);
        
        // retrieve the sorting from the db
        var startingOrder = RetrieveSortOrder(sortOrderId, db);
        
        // prepare the data for moving
        var sortedSourceItems = itemsToMove
            .Select(key => startingOrder.FirstOrOptional(item => item.Key.Equals(key)))
            .Where(item => {
                if (!item.HasValue)
                    _logger.LogWarning("Unable to move Item: {ItemKey} not found in sort order {SortOrderId}", item.Value.Key, sortOrderId);
                return item.HasValue;
            })
            .Select(item => item.Value)
            .OrderBy(item => item.SortIndex)
            .ToArray();
        
        var stagingList = startingOrder.ToList();
        
        // Determine the index of the drop target item
        var dropTargetIndex = stagingList.FindIndex(item => item.Key.Equals(dropTargetItem));
        if (dropTargetIndex == -1)
        {
            _logger.LogWarning("Drop target item {DropTargetItem} not found in sort order {SortOrderId}", dropTargetItem, sortOrderId);
            return;
        }
        
        var targetIndex = relativePosition == TargetRelativePosition.BeforeTarget ? dropTargetIndex : dropTargetIndex + 1;
        
        var insertPositionIndex = targetIndex;
        
        // Adjust the insert position index to account for any items before the target index that are also being moved
        foreach (var item in sortedSourceItems)
        {
            if (!(item.SortIndex < targetIndex))
                break;
            insertPositionIndex--;
        }
        
        // Remove items from the staging list and insert them at the new adjusted position
        stagingList.Remove(sortedSourceItems);
        stagingList.InsertRange(insertPositionIndex, sortedSourceItems);
        
        // Update the sort index of all items
        for (var i = 0; i < stagingList.Count; i++)
        {
            var item = stagingList[i];
            item.SortIndex = i;
        }
        
        if (token.IsCancellationRequested) return;
        
        // persist the new sorting
        await PersistSortOrder(stagingList, sortOrderId, token);
    }

    /// <inheritdoc />
    public async ValueTask MoveItemDelta(SortOrderId sortOrderId, TKey sourceItem, int delta, IDb? db = null, CancellationToken token = default)
    {
        // acquire the lock
        using var _ = await _manager.Lock(token);
        
        // retrieve the sorting from the db
        var startingOrder = RetrieveSortOrder(sortOrderId, db);
        var stagingList = startingOrder.ToList();
        
        // get the index of the source item
        var foundItem = stagingList.FirstOrOptional(item => item.Key.Equals(sourceItem));
        if (!foundItem.HasValue)
        {
            _logger.LogWarning("Source item {SourceItem} not found in sort order {SortOrderId}", sourceItem, sortOrderId);
            return;
        }
        var sortableItem = foundItem.Value;
        var currentIndex = sortableItem.SortIndex;
        
        var newIndex = currentIndex + delta;
        
        // Ensure the new index is within bounds
        newIndex = Math.Clamp(newIndex, 0, stagingList.Count - 1);
        if (newIndex == currentIndex) return;
        
        
        // Move the item in the list
        stagingList.RemoveAt(currentIndex);
        stagingList.Insert(newIndex, sortableItem);
            
        // Update the sort index of all items
        for (var i = 0; i < stagingList.Count; i++)
        {
            var item = stagingList[i]
            item.SortIndex = i;
        }
            
        if (token.IsCancellationRequested) return;
            
        await PersistSortOrder(stagingList, sortOrderId, token);
    }

    /// <inheritdoc />
    public abstract ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default);
    
    #endregion public members
    
    #region protected members
    
    /// <summary>
    /// Retrieves the sortable entries for the sortOrderId, and returns them as a list of sortable items.
    /// </summary>
    /// <remarks>
    /// The items in the returned list can have temporary values for properties such as `ModName` and `IsActive`.
    /// Those will need to be updated after the sortableItems are matched to items in the loadout. 
    /// </remarks>
    protected abstract IReadOnlyList<TItem> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb? db = null);
    
    /// <summary>
    /// Persists the sort order for the provided list of sortable items
    /// </summary>
    protected abstract Task PersistSortOrder(IReadOnlyList<TItem> items, SortOrderId sortOrderEntityId, CancellationToken token);
    
    #endregion protected members
}
