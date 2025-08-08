using System.ComponentModel;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Abstract base class for a variety of sort order for a specific game.
/// </summary>
/// <typeparam name="TKey">The type of the key used by the game to identify sortable items</typeparam>
/// <typeparam name="TSortableItem">The type of the SortableItem implementation used for this variety</typeparam>
/// <typeparam name="TItemLoadoutData">Represents an item in the loadout that can be sorted but without the sorting information</typeparam>
/// <typeparam name="TSortedEntry">Represents the minimal information for persisting a sortable entry inside a Sort Order</typeparam>
public abstract class ASortOrderVariety<TKey, TSortableItem, TItemLoadoutData, TSortedEntry> : ISortOrderVariety<TKey, TSortableItem>
    where TKey : IEquatable<TKey>, ISortItemKey
    where TSortableItem : IReactiveSortItem<TSortableItem, TKey>
    where TItemLoadoutData : ISortItemLoadoutData<TKey>
    where TSortedEntry : ISortItemData<TKey>
{
    private readonly ILogger _logger;
    
    /// <summary>
    /// Database connection
    /// </summary>
    protected readonly IConnection Connection;
    
    /// <summary>
    /// The game sort order manager that this variety belongs to.
    /// </summary>
    protected readonly ISortOrderManager Manager;
    
    protected ASortOrderVariety(IServiceProvider serviceProvider, ISortOrderManager manager)
    {
        Connection = serviceProvider.GetRequiredService<IConnection>();
        _logger = serviceProvider.GetRequiredService<ILogger>();
        Manager = manager;
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
    public Optional<SortOrderId> GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity)
    {
        var entities = SortOrder.FindByParentEntity(Connection.Db, parentEntity)
            .Where(e => e.SortOrderTypeId == SortOrderVarietyId.Value)
            .ToArray();
        
        switch (entities.Length)
        {
            case 0:
                return Optional<SortOrderId>.None;
            case > 1:
                _logger.LogWarning("Multiple SortOrder entities found for parent entity {ParentEntity} and variety {VarietyId}", parentEntity, SortOrderVarietyId);
                break;
        }

        return entities[0].SortOrderId;
    }

    /// <inheritdoc />
    public abstract ValueTask<SortOrderId> GetOrCreateSortOrderFor(
        LoadoutId loadoutId,
        OneOf<LoadoutId, CollectionGroupId> parentEntity,
        CancellationToken token = default);

    /// <inheritdoc />
    public abstract IObservable<IChangeSet<TSortableItem, TKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId);

    /// <inheritdoc />
    public abstract IReadOnlyList<TSortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db);

    /// <inheritdoc />
    public async ValueTask MoveItems(
        SortOrderId sortOrderId,
        TKey[] itemsToMove,
        TKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db = null,
        CancellationToken token = default)
    {
        var dbToUse = db ?? Connection.Db;
        // retrieve the sorting from the db
        var startingOrder = RetrieveSortOrder(sortOrderId, dbToUse);
        
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
        
        // TODO: Should we retry if the transaction fails due to a data race?
        await TryPersistSortOrder(sortOrderId, stagingList, dbToUse, token);
    }

    /// <inheritdoc />
    public async ValueTask MoveItemDelta(SortOrderId sortOrderId, TKey sourceItem, int delta, IDb? db = null, CancellationToken token = default)
    {
        var dbToUse = db ?? Connection.Db;
        
        // retrieve the sorting from the db
        var startingOrder = RetrieveSortOrder(sortOrderId, dbToUse);
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
            var item = stagingList[i];
            item.SortIndex = i;
        }
            
        if (token.IsCancellationRequested) return;
        
        // TODO: Should we retry if the transaction fails due to a data race?
        await TryPersistSortOrder(sortOrderId, stagingList, dbToUse, token);
    }

    /// <inheritdoc />
    public virtual async ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default)
    {
        // If this is passed a specific database, don't retry the reconciliation using the most recent database.
        var noRetry = db is not null; 
        var retryCount = 0;
        var dbToUse = db ?? Connection.Db;

        while (retryCount <= 3)
        {
            var reconciledItems = ReconcileSortOrderCore(sortOrderId, dbToUse);
            
            var succeded = await TryPersistSortOrder(sortOrderId, reconciledItems.Select(tuple => tuple.SortedEntry).ToArray(), dbToUse, token);
            if (succeded) return;
        
            if (noRetry)
            {
                _logger.LogWarning("Reconciliation of sort order {SortOrderId} failed, but no retry is allowed", sortOrderId);
                return;
            }
            
            retryCount++;
            _logger.LogWarning("Reconciliation of sort order {SortOrderId} failed, retrying ({RetryCount}/3)", sortOrderId, retryCount);
        }
        
        _logger.LogError("Reconciliation of sort order {SortOrderId} failed after 4 attempts", sortOrderId);
    }
    

#endregion public members
    
#region protected members
    
    /// <summary>
    /// Retrieves the sorted entries for the sortOrderId, and returns them as a sorted list of TSortedEntries, without the loadout data.
    /// </summary>
    protected abstract IReadOnlyList<TSortedEntry> RetrieveSortOrder(SortOrderId sortOrderId, IDb db);

    /// <summary>
    /// Persists the sort order for the provided list of sortable items.
    /// Returns false if the transaction fails due to a data race condition.
    /// </summary>
    protected virtual async ValueTask<bool> TryPersistSortOrder(SortOrderId sortOrderId, IReadOnlyList<TSortedEntry> newOrder, IDb startingDb, CancellationToken token = default)
    {
        using var tx = Connection.BeginTransaction();
        
        PersistSortOrderCore(sortOrderId, newOrder, tx, startingDb, token);

        var sortOrderTxId = GetMaxTxIdForSortOrder(sortOrderId, startingDb);
            
        tx.Add(sortOrderId, sortOrderTxId,
            (_ ,txDb, orderId, oldTxId) =>
            {
                var currentTxId = GetMaxTxIdForSortOrder(orderId, txDb);
                
                if (currentTxId > oldTxId)
                {
                    throw new InvalidOperationException(
                        $"Unable to complete transaction: Sort Order {orderId} changed, Current TxId: {currentTxId}, Old TxId: {oldTxId}");
                }
            } );
        
        try
        {
            await tx.Commit();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to persist sort order {SortOrderId}", sortOrderId);
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Prepares the transaction to persist the sort order for the provided list of sortable entries.
    /// </summary>
    /// <param name="sortOrderId">Id of the sort order</param>
    /// <param name="newOrder">A sorted list of entries representing the new order to be persisted</param>
    /// <param name="tx">The transaction to be used. Will not be committed</param>
    /// <param name="startingDb">Db revision to base change on</param>
    /// <param name="token"></param>
    protected abstract void PersistSortOrderCore(
        SortOrderId sortOrderId,
        IReadOnlyList<TSortedEntry> newOrder,
        ITransaction tx,
        IDb startingDb,
        CancellationToken token = default);
    
    
    protected virtual IReadOnlyList<(TSortedEntry SortedEntry, TItemLoadoutData ItemLoadoutData)> ReconcileSortOrderCore(SortOrderId sortOrderId, IDb dbToUse)
    {
        var sortOrder = SortOrder.Load(dbToUse, sortOrderId);
        
        var collectionGroupId = sortOrder.ParentEntity.IsT1 ? 
            sortOrder.ParentEntity.AsT1 : 
            Optional<CollectionGroupId>.None;
        
        var loadoutData = RetrieveLoadoutData(sortOrder.LoadoutId, collectionGroupId, dbToUse);
        
        var currentSortOrder = RetrieveSortOrder(sortOrderId, dbToUse);
        
        var reconciledItems = Reconcile(currentSortOrder, loadoutData);
        return reconciledItems;
    }
    
    /// <summary>
    /// Returns a collection of loadout-specific TItemLoadoutData for each relevant item found in the provided loadout/collection.
    /// These loadout data items are unsorted and do not have a sort index.
    /// </summary>
    protected abstract IReadOnlyList<TItemLoadoutData> RetrieveLoadoutData(
        LoadoutId loadoutId,
        Optional<CollectionGroupId> collectionGroupId,
        IDb? db);

    /// <summary>
    /// Reconciles the provided sorted entries with the loadout data items returning a list of sortable items that include loadout data.
    /// </summary>
    protected abstract IReadOnlyList<(TSortedEntry SortedEntry, TItemLoadoutData ItemLoadoutData)> Reconcile(
        IReadOnlyList<TSortedEntry> sourceSortedEntries,
        IReadOnlyList<TItemLoadoutData> loadoutDataItems);
    
    
    protected static TxId GetMaxTxIdForSortOrder(SortOrderId sortOrderId, IDb db)
    {
        // TODO: Use better queries to potentially optimize this
        
        var maxTxId = db.Get(sortOrderId).Max(datom => datom.T);
        var maxIds = db.Datoms(SortOrderItem.ParentSortOrder, sortOrderId)
            .Select(d => db.Get(d.E).Max(datom => datom.T));
        
        foreach (var maxId in maxIds)
        {
            if (maxId < maxTxId)
                maxTxId = maxId;
        }
        return maxTxId;
    }

#endregion protected members
}
