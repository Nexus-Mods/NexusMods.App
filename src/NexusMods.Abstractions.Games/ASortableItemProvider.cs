using System.Collections.Immutable;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProvider<TItem, TKey> : ILoadoutSortableItemProvider<TItem, TKey>
    where TItem : ISortableItem<TItem, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    private bool _isDisposed;
    
    /// <summary>
    /// Time to wait for the semaphore before timing out
    /// </summary>
    protected readonly TimeSpan SemaphoreTimeout = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Async semaphore for serializing changes to the sort order
    /// </summary>
    protected readonly SemaphoreSlim Semaphore = new(1, 1);
    
    /// <summary>
    /// Source cache of the sortable items used to expose the latest sort order
    /// </summary>
    protected readonly SourceCache<TItem, TKey> OrderCache = new(item => item.Key);

    /// <summary>
    /// EntityId for the main SortOrder database entity that this provider is associated with.
    /// Used to persist and retrieve the order of items.
    /// </summary>
    protected readonly SortOrderId SortOrderEntityId;
    
    /// <summary>
    /// Protected constructor, use CreateAsync method to create an instance
    /// </summary>
    protected ASortableItemProvider(ISortableItemProviderFactory parentFactory, LoadoutId loadoutId, SortOrderId sortOrderId)
    {
        ParentFactory = parentFactory;
        LoadoutId = loadoutId;
        SortOrderEntityId = sortOrderId;
        
        SortableItemsChangeSet = OrderCache.Connect().RefCount();
    }

    /// <inheritdoc />
    public ISortableItemProviderFactory ParentFactory { get; }

    /// <inheritdoc />
    public LoadoutId LoadoutId { get; }

    /// <inheritdoc />
    public IObservable<IChangeSet<TItem, TKey>> SortableItemsChangeSet { get; }

    /// <inheritdoc />
    public IReadOnlyList<TItem> GetCurrentSorting()
    {
        return OrderCache.Items
            .OrderBy(item => item.SortIndex)
            .ToImmutableArray();
    }

    /// <Inheritdoc />
    public Optional<TItem> GetSortableItem(TKey itemId)
    {
        return OrderCache.Lookup(itemId);
    }

    /// <Inheritdoc />
    public virtual async Task SetRelativePosition(TItem sortableItem, int delta, CancellationToken token)
    {
        var hasEntered = await Semaphore.WaitAsync(SemaphoreTimeout, token);
        if (!hasEntered) throw new TimeoutException($"Timed out waiting for semaphore in SetRelativePosition");
        
        try
        {
            // Get a stagingList of the items in the order
            var stagingList = OrderCache.Items
                .OrderBy(item => item.SortIndex)
                .ToList();

            // Get the current index of the item relative to the full list
            var currentIndex = stagingList.IndexOf(sortableItem);

            // Get the new index of the group relative to the full list
            var newIndex = currentIndex + delta;

            // Ensure the new index is within the bounds of the list
            newIndex = Math.Clamp(newIndex, 0, stagingList.Count - 1);
            if (newIndex == currentIndex) return;

            // Move the item in the list
            stagingList.RemoveAt(currentIndex);
            stagingList.Insert(newIndex, sortableItem);
            
            // Update the sort index of all items
            for (var i = 0; i < stagingList.Count; i++)
            {
                stagingList[i].SortIndex = i;
            }
            
            if (token.IsCancellationRequested) return;
            
            await PersistSortOrder(stagingList, SortOrderEntityId, token);

            OrderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <Inheritdoc />
    public virtual async Task MoveItemsTo(
        TItem[] sourceItems,
        TItem targetItem,
        TargetRelativePosition relativePosition,
        CancellationToken token)
    {
        var hasEntered = await Semaphore.WaitAsync(SemaphoreTimeout, token);
        if (!hasEntered) throw new TimeoutException($"Timed out waiting for semaphore in MoveItemsTo");
        
        try
        {
            // Sort the source items to move by their sort index
            var sortedSourceItems = sourceItems
                .OrderBy(item => item.SortIndex)
                .ToArray();
            
            // Get current ordering from cache
            var stagingList = OrderCache.Items
                .OrderBy(item => item.SortIndex)
                .ToList();
            
            // Determine target insertion position
            var targetItemIndex = stagingList.IndexOf(targetItem);
            var targetIndex = relativePosition == TargetRelativePosition.BeforeTarget ? targetItemIndex : targetItemIndex + 1;

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
                stagingList[i].SortIndex = i;
            }
            
            if (token.IsCancellationRequested) return;
            
            // Update the database
            await PersistSortOrder(stagingList, SortOrderEntityId, token);
            
            // Update the public cache
            OrderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<TItem>> RefreshSortOrder(CancellationToken token, IDb? loadoutDb = null);

    /// <summary>
    /// Persists the sort order for the provided list of sortable items
    /// </summary>
    protected abstract Task PersistSortOrder(IReadOnlyList<TItem> items, SortOrderId sortOrderEntityId, CancellationToken token);
    
    /// <summary>
    /// Retrieves the sortable entries for the sortOrderId, and returns them as a list of sortable items.
    /// </summary>
    /// <remarks>
    /// The items in the returned list can have temporary values for properties such as `ModName` and `IsActive`.
    /// Those will need to be updated after the sortableItems are matched to items in the loadout. 
    /// </remarks>
    protected abstract IReadOnlyList<TItem> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb? db = null);

    /// <summary>
    /// Obtains the SortOrder model for this provider if it exists, or creates a new one if it doesn't.
    /// If class implementations are using derived SortModels, they should implement and use a custom alternative
    /// </summary>
    protected static async ValueTask<SortOrder.ReadOnly> GetOrAddSortOrderModel(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory factory)
    {
        var sortOrder = SortOrder.All(connection.Db)
            .FirstOrOptional(lo => lo.LoadoutId == loadoutId
                                   && lo.SortOrderTypeId == factory.SortOrderTypeId
            );

        if (sortOrder.HasValue)
            return sortOrder.Value;

        using var ts = connection.BeginTransaction();
        _ = new SortOrder.New(ts)
        {
            LoadoutId = loadoutId,
            SortOrderTypeId = factory.SortOrderTypeId,
        };

        await ts.Commit();

        return sortOrder.Value;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Dispose(true);
    }
    
    /// <summary>
    /// Disposes base class
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            Semaphore.Dispose();
            OrderCache.Dispose();
        }

        _isDisposed = true;
    }
    
}
