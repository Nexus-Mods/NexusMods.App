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
/// <typeparam name="TKey">The type of the key used by the game to identify sort order items</typeparam>
/// <typeparam name="TReactiveSortItem">The type of the ReactiveSortItem implementation used for this variety</typeparam>
/// <typeparam name="TItemLoadoutData">Represents an item in the loadout that can be sorted but without the sorting information</typeparam>
/// <typeparam name="TItemSortData">Represents the minimal information for persisting a sort order item inside a Sort Order</typeparam>
public abstract class ASortOrderVariety<TKey, TReactiveSortItem, TItemLoadoutData, TItemSortData> : ISortOrderVariety<TKey, TReactiveSortItem>
    where TKey : IEquatable<TKey>, ISortItemKey
    where TReactiveSortItem : IReactiveSortItem<TReactiveSortItem, TKey>
    where TItemLoadoutData : ISortItemLoadoutData<TKey>
    where TItemSortData : ISortItemData<TKey>
{
    private readonly ILogger<ASortOrderVariety<TKey, TReactiveSortItem, TItemLoadoutData, TItemSortData>> _logger;
    
    /// <summary>
    /// Database connection
    /// </summary>
    protected readonly IConnection Connection;

    protected ASortOrderVariety(IServiceProvider serviceProvider)
    {
        Connection = serviceProvider.GetRequiredService<IConnection>();
        _logger = serviceProvider.GetRequiredService<ILogger<ASortOrderVariety<TKey, TReactiveSortItem, TItemLoadoutData, TItemSortData>>>();
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
    public Optional<SortOrderId> GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity, IDb? db = null)
    {
        var dbToUse = db ?? Connection.Db;
        
        var entities = SortOrder.FindByParentEntity(dbToUse, parentEntity)
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
    public abstract IObservable<IChangeSet<TReactiveSortItem, TKey>> GetSortOrderItemsChangeSet(SortOrderId sortOrderId);

    /// <inheritdoc />
    public abstract IReadOnlyList<TReactiveSortItem> GetSortOrderItems(SortOrderId sortOrderId, IDb? db);

    /// <inheritdoc />
    public async Task MoveItems(
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
    public async Task MoveItemDelta(SortOrderId sortOrderId, TKey sourceItem, int delta, IDb? db = null, CancellationToken token = default)
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
        var sortOrderItem = foundItem.Value;
        var currentIndex = sortOrderItem.SortIndex;
        
        var newIndex = currentIndex + delta;
        
        // Ensure the new index is within bounds
        newIndex = Math.Clamp(newIndex, 0, stagingList.Count - 1);
        if (newIndex == currentIndex) return;
        
        
        // Move the item in the list
        stagingList.RemoveAt(currentIndex);
        stagingList.Insert(newIndex, sortOrderItem);
            
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
    public virtual async ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? referenceDb = null, CancellationToken token = default)
    {
        // If this is passed a specific database, don't retry the reconciliation using the most recent database.
        var noRetry = referenceDb is not null; 
        var retryCount = 0;

        while (retryCount <= 3)
        {
            var refDb = referenceDb ?? Connection.Db;

            // Check if the sort order still exists
            if (!SortOrder.Load(refDb.Connection.Db, sortOrderId).IsValid())
            {
                // Sort order no longer exists, collection/loadout was deleted, abort.
                return;
            }
            
            var reconciledItems = ReconcileSortOrderCore(sortOrderId, refDb);
            
            var succeeded = await TryPersistSortOrder(sortOrderId, reconciledItems.Select(tuple => tuple.SortedEntry).ToArray(), refDb, token);
            if (succeeded) return;
        
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
    
    /// <inheritdoc />
    public virtual async ValueTask DeleteSortOrder(SortOrderId sortOrderId, CancellationToken token = default)
    {
        var tx = Connection.BeginTransaction();
        
        // Delete the items
        foreach (var item in Connection.Db.Datoms(SortOrderItem.ParentSortOrder, sortOrderId))
        {
            tx.Delete(item.E, recursive: false);
        }
        
        // Delete the sort order
        tx.Delete(sortOrderId, recursive: false);
        
        try
        {
            await tx.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sort order {SortOrderId}", sortOrderId);
        }
    }
    

#endregion public members
    
#region protected members
    
    /// <summary>
    /// Retrieves the sorted entries for the sortOrderId, and returns them as a sorted list of TSortedEntries, without the loadout data.
    /// </summary>
    protected abstract IReadOnlyList<TItemSortData> RetrieveSortOrder(SortOrderId sortOrderId, IDb db);

    /// <summary>
    /// Persists the sort order for the provided list of sort items.
    /// Returns false if the transaction fails due to a data race condition.
    /// </summary>
    protected virtual async ValueTask<bool> TryPersistSortOrder(SortOrderId sortOrderId, IReadOnlyList<TItemSortData> newOrder, IDb startingDb, CancellationToken token = default)
    {
        var tx = Connection.BeginTransaction();
        
        PersistSortOrderCore(sortOrderId, newOrder, tx, startingDb, token);

        var sortOrderTxId = GetMaxTxIdForSortOrder(sortOrderId, startingDb) ;
        if (sortOrderTxId == default(TxId)) return false;
        
        tx.Add(new SortOrderFailsafe(sortOrderId, sortOrderTxId));
        
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
    
    private class SortOrderFailsafe(EntityId sortOrderId, TxId lastSeenTx) : ITxFunction
    {
        public void Apply(Transaction tx)
        {
            var currentTxId = GetMaxTxIdForSortOrder(sortOrderId, tx.BasisDb);
                
            if (currentTxId > lastSeenTx || currentTxId == default(TxId))
            {
                throw new InvalidOperationException(
                    $"Unable to complete transaction: Sort Order {sortOrderId} changed, Current TxId: {currentTxId}, Old TxId: {lastSeenTx}");
            }
        }
    }

    /// <summary>
    /// Prepares the transaction to persist the sort order for the provided list of sort items.
    /// </summary>
    /// <param name="sortOrderId">Id of the sort order</param>
    /// <param name="newOrder">A sorted list of entries representing the new order to be persisted</param>
    /// <param name="tx">The transaction to be used. Will not be committed</param>
    /// <param name="startingDb">Db revision to base change on</param>
    /// <param name="token"></param>
    protected abstract void PersistSortOrderCore(
        SortOrderId sortOrderId,
        IReadOnlyList<TItemSortData> newOrder,
        Transaction tx,
        IDb startingDb,
        CancellationToken token = default);
    
    
    protected virtual IReadOnlyList<(TItemSortData SortedEntry, TItemLoadoutData ItemLoadoutData)> ReconcileSortOrderCore(SortOrderId sortOrderId, IDb loadoutRevisionDb)
    {
        // Get the most recent sort order data
        var sortOrder = SortOrder.Load(loadoutRevisionDb.Connection.Db, sortOrderId);
        
        var collectionGroupId = sortOrder.ParentEntity.IsT1 ? 
            sortOrder.ParentEntity.AsT1 : 
            Optional<CollectionGroupId>.None;
        
        // Get the loadout data from the revision db
        var loadoutData = RetrieveLoadoutData(sortOrder.LoadoutId, collectionGroupId, loadoutRevisionDb);
        
        // Get the most recent sort order data
        var currentSortOrder = RetrieveSortOrder(sortOrderId, loadoutRevisionDb.Connection.Db);
        
        var reconciledItems = Reconcile(currentSortOrder, loadoutData);
        return reconciledItems;
    }
    
    /// <summary>
    /// Returns a collection of loadout-specific TItemLoadoutData for each relevant item found in the provided loadout/collection.
    /// These loadout data items are unsorted and do not have a sort index.
    /// This already filters out duplicates, currently selecting the most recently added enabled item for each key.
    /// </summary>
    protected abstract IReadOnlyList<TItemLoadoutData> RetrieveLoadoutData(
        LoadoutId loadoutId,
        Optional<CollectionGroupId> collectionGroupId,
        IDb? db);

    /// <summary>
    /// Reconciles the provided sorted entries with the loadout data items returning a list of sort items that include loadout data.
    /// </summary>
    protected abstract IReadOnlyList<(TItemSortData SortedEntry, TItemLoadoutData ItemLoadoutData)> Reconcile(
        IReadOnlyList<TItemSortData> sourceSortedEntries,
        IReadOnlyList<TItemLoadoutData> loadoutDataItems);
    
    
    protected static TxId GetMaxTxIdForSortOrder(SortOrderId sortOrderId, IDb db)
    {
        var sortOrder = SortOrder.Load(db, sortOrderId);
        if (!sortOrder.IsValid()) return default(TxId);
        
        var maxTxId = db[sortOrderId].Max(datom => datom.T);
        var maxIds = db.Datoms(SortOrderItem.ParentSortOrder, sortOrderId)
            .Select(d => db[d.E].Max(datom => datom.T));
        
        foreach (var maxId in maxIds)
        {
            if (maxId > maxTxId)
                maxTxId = maxId;
        }
        return maxTxId;
    }

#endregion protected members
}
