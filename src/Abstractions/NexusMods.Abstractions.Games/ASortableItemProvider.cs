using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using FomodInstaller.Utils.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using R3;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProvider<TObject> : ILoadoutSortableItemProvider
    where TObject : notnull
{
    private readonly IConnection _connection;
    private readonly SourceCache<ISortableItem, Guid> _orderCache = new(item => item.ItemId);
    private readonly ReadOnlyObservableCollection<ISortableItem> _readOnlyOrderList;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CompositeDisposable _disposables = new();

    protected abstract SortOrderId SortOrderId { get; }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<ISortableItem> SortableItems => _readOnlyOrderList;

    /// <inheritdoc />
    public IObservable<IChangeSet<ISortableItem, Guid>> SortableItemsChangeSet { get; }
    
    /// <summary>
    /// Other observable changes we want to listen to.
    /// </summary>
    /// <returns></returns>
    protected abstract IObservable<IChangeSet<TObject, EntityId>> GetObservableChanges();
    
    /// <inheritdoc />
    public LoadoutId LoadoutId { get; }

    /// <inheritdoc />
    public ISortableItemProviderFactory ParentFactory { get; }

    /// <inheritdoc />
    public abstract ISortableItem CreateSortableItem(IConnection connection, LoadoutId loadoutId, Guid id, int idx);

    /// <inheritdoc />
    protected abstract List<ISortableItem> GetSortableEntries(IDb? db = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    protected abstract List<SortableEntry.ReadOnly> GetPersistentEntries(IDb? db = null);
    
    protected virtual async Task PersistSortableItems(List<ISortableItem> orderList, CancellationToken token)
    {
        var availableMods = GetSortableEntries(_connection.Db);
        var persistentSortableItems = GetPersistentEntries(_connection.Db);
        
        if (token.IsCancellationRequested) return;
        
        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            var liveItem = availableMods.FirstOrOptional(
                i => i.ItemId == dbItem.ItemId
            );
            
            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }
            
            var liveIdx = orderList.IndexOf(liveItem.Value);

            if (dbItem.SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < orderList.Count; i++)
        {
            var liveItem = orderList[i];
            if (persistentSortableItems.Any(si => si.ItemId == liveItem.ItemId))
                continue;

            _ = new SortableEntry.New(tx)
            {
                ItemId = liveItem.ItemId,
                ParentSortOrderId = SortOrderId,
                SortIndex = i,
            };
        }

        if (token.IsCancellationRequested) return;

        await tx.Commit();
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="loadoutId"></param>
    /// <param name="id"></param>
    /// <param name="parentFactory"></param>
    protected ASortableItemProvider(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        _connection = connection;
        LoadoutId = loadoutId;
        ParentFactory = parentFactory;

        // load the previously saved order
        var order = GetSortableEntries(_connection.Db);
        _orderCache.AddOrUpdate(order);
        
        // populate read only list
        _orderCache.Connect()
            .Transform(item => item)
            .SortBy(item => item.SortIndex)
            .Bind(out _readOnlyOrderList)
            .Subscribe()
            .AddTo(_disposables);
        
        SortableItemsChangeSet = SortableItems.ToObservableChangeSet(item => item.ItemId).RefCount();
    }

    /// <summary>
    /// 
    /// </summary>
    protected void RegisterObservables()
    {
        GetObservableChanges()
            .ChangeKey(x => GenerateGuid(x))
            .ToObservable()
            .SubscribeAwait(
                async (changes, token) => { await UpdateOrderCache(changes, token); },
                awaitOperation: AwaitOperation.Sequential
            )
            .AddTo(_disposables);
    }

    /// <inheritdoc />
    public virtual Optional<ISortableItem> GetSortableItem(Guid id)
    {
        return SortableItems.FirstOrOptional(item => item.ItemId.Equals(id));
    }

    /// <inheritdoc />
    public async Task SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            // Get a stagingList of the items in the order
            var stagingList = _orderCache.Items
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
            
            await PersistSortableItems(stagingList, token);

            _orderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <inheritdoc/>
    public async Task MoveItemsTo(ISortableItem[] sourceItems, ISortableItem targetItem, TargetRelativePosition relativePosition, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            // Sort the source items to move by their sort index
            var sortedSourceItems = sourceItems
                .OrderBy(item => item.SortIndex)
                .ToArray();
            
            // Get current ordering from cache
            var stagingList = _orderCache.Items
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
            await PersistSortableItems(stagingList, token);
            
            // Update the public cache
            _orderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="changes"></param>
    /// <param name="token"></param>
    private async Task UpdateOrderCache(IChangeSet<TObject, Guid> changes, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var added = changes.Where(x => x.Reason == ChangeReason.Add);
            var updated = changes.Where(x => x.Reason == ChangeReason.Update);
            var removed = changes.Where(x => x.Reason == ChangeReason.Remove);
            var allChanges = added.Concat(updated).Concat(removed)
                .Select(x => x.Current)
                .ToList();
            var availableMods = GetSortableEntries(_connection.Db);
            var currentOrder = _orderCache.Items.OrderBy(item => item.SortIndex);
            
            if (token.IsCancellationRequested) return;
            
            // Update the order
            var stagingList = SynchronizeSortingToItems(currentOrder.ToList(), allChanges, this);
            
            if (token.IsCancellationRequested) return;

            // Update the database
            await PersistSortableItems(stagingList, token);
            
            if (token.IsCancellationRequested) return;

            // Update the cache
            _orderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// This method generates a new order list from currentOrder, after removing items that are no longer available and
    /// adding new items that have become available.
    /// New items are added at the beginning of the list, to make them win over existing items.
    /// </summary>
    /// <param name="availableMods">Collection of mods to synchronize against</param>
    /// <param name="currentOrder">The starting order</param>
    /// <returns>The new sorting</returns>
    protected List<ISortableItem> SynchronizeSortingToItems(
        List<ISortableItem> currentOrder,
        List<TObject> availableMods,
        ASortableItemProvider<TObject> provider)
    {
        var modsToAdd = new List<Guid>();
        var sortableItemsToRemove = new List<ISortableItem>();
        
        // Find items to remove
        foreach (var si in currentOrder)
        {
            // TODO: determine the winning mod in case of multiple mods with the same name
            var modMatch = availableMods.FirstOrOptional(g => GenerateGuid(g).Equals(si.ItemId));
            if (!modMatch.HasValue)
            {
                sortableItemsToRemove.Add(si);
            }
        }

        // Find items to add
        foreach (var mod in availableMods)
        {
            var sortableItem = currentOrder.FirstOrOptional(item =>
                item.ItemId.Equals(GenerateGuid(mod)));

            if (!sortableItem.HasValue)
            {
                modsToAdd.Add(GenerateGuid(mod));
            }
        }

        // Get a staging list of the items to make changes to
        var stagingList = currentOrder
            .OrderBy(item => item.SortIndex)
            .ToList();

        stagingList.Remove(sortableItemsToRemove);
        
        // Insert based on the sort index behavior (i.e. smaller index wins or greater index wins)
        //  we want the items to be added as "winners" out of the box.
        var insertIdx = ParentFactory.IndexOverrideBehavior.Equals(IndexOverrideBehavior.GreaterIndexWins) ? modsToAdd.Capacity : 0;
        stagingList.InsertRange(insertIdx,
            modsToAdd.Select((id, idx) =>
                provider.CreateSortableItem(_connection, LoadoutId, id, idx)));

        for (var i = 0; i < stagingList.Count; i++)
        {
            var item = stagingList[i];
            item.SortIndex = i;
        }
        
        return stagingList;
    }

    protected virtual async ValueTask<SortOrder.ReadOnly> GetOrAddSortOrderModel()
    {
        var sortOrder = SortOrder.All(_connection.Db)
            .FirstOrOptional(lo => lo.LoadoutId == LoadoutId
                                   && lo.SortOrderTypeId == ParentFactory.SortOrderTypeId);

        if (sortOrder.HasValue)
            return sortOrder.Value;

        using var ts = _connection.BeginTransaction();
        _ = new SortOrder.New(ts)
        {
            LoadoutId = LoadoutId,
            SortOrderTypeId = ParentFactory.SortOrderTypeId,
        };

        await ts.Commit();
        
        return sortOrder.Value;
    }
    
    private static Dictionary<string, Guid> _guidCache = new();
    protected static Guid GenerateGuid(dynamic input)
    {
        var guidEntry = input.ToString();
        if (_guidCache.ContainsKey(guidEntry))
            return _guidCache[guidEntry];
        
        using var hashAlgorithm = System.Security.Cryptography.SHA256.Create();
        var hash = hashAlgorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(guidEntry));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        var guid = new Guid(guidBytes);
        _guidCache[guidEntry] = guid;
        return new Guid(guidBytes);
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        _semaphore.Dispose();
        _orderCache.Dispose();
    }
}
