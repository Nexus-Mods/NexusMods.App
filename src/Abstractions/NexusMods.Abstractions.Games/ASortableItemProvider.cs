using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games.Interfaces;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using R3;
using Observable = R3.Observable;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// A loadout specific provider and manager of sortable items.
/// </summary>
/// <typeparam name="TItem">Type representing an item being sorted</typeparam>
public abstract class ASortableItemProvider<TItem> : ILoadoutSortableItemProvider
    where TItem : ISortableDbEntryConstraints
{
    private readonly IConnection _connection;
    private readonly SourceCache<ISortableItem, Guid> _orderCache = new(item => item.ItemId);
    private readonly ReadOnlyObservableCollection<ISortableItem> _readOnlyOrderList;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CompositeDisposable _disposables = new();

    private Guid _sortOrderTypeId;
    public Guid SortOrderTypeId { get; }

    private SortOrderId _sortOrderId;
    public SortOrderId SortOrderId { get; }

    /// <inheritdoc />
    public LoadoutId LoadoutId { get; }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<ISortableItem> SortableItems => _readOnlyOrderList;

    /// <inheritdoc />
    public IObservable<IChangeSet<ISortableItem, Guid>> SortableItemsChangeSet { get; }

    /// <summary>
    /// Returns an observable that will trigger when there are relevant changes to items being sorted,
    /// such as additions or removals.
    /// </summary>
    /// <remarks>
    /// This only indicates that the items have changed, but not the nature of the change,
    /// which could lead to inefficient computation of changes and updates, but can be more reliable.
    /// </remarks>
    protected abstract IObservable<bool> GetItemsChangedObservable();

    /// <inheritdoc />
    public ISortableItemProviderFactory ParentFactory { get; }

    /// <summary>
    ///  Create a sortableItem from the given persistent item. 
    /// </summary>
    protected abstract ISortableItem CreateSortableItem(IConnection connection, LoadoutId loadoutId, TItem item, int idx);

    /// <summary>
    /// The persistent entries we have in the database. This can be any type
    ///  of data we want as long as we can identify the sortable item that goes
    ///  along with it + the SortableEntityId as stored in the db.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    protected abstract List<TItem> GetPersistentEntries(IDb? db = null);

    /// <summary>
    /// Returns the current list of sortable items from the cache, sorted by sort index.
    /// </summary>
    public List<ISortableItem> GetSortableEntries()
    {
        return _orderCache.Items.OrderBy(x => x.SortIndex).ToList();
    }
    
    /// <summary>
    /// Protected constructor.
    /// It is recommended to use static CreateAsync methods to create instances of this class with the correct initial state.
    /// </summary>
    protected ASortableItemProvider(
        IConnection connection,
        SortOrder.ReadOnly sortOrderModel,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        _connection = connection;
        LoadoutId = loadoutId;
        ParentFactory = parentFactory;
        _sortOrderTypeId = sortOrderModel.SortOrderTypeId;
        _sortOrderId = sortOrderModel.SortOrderId;

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
    /// Takes the current persistent entries and generates a new list of sortable items
    /// </summary>
    /// <returns>A collection of generated ISortableItems</returns>
    protected virtual IEnumerable<ISortableItem> GenerateSortableItems()
    {
        // Get the persistent entries from the db
        var persistentEntries = GetPersistentEntries(_connection.Db)
            .OrderBy(x => x.SortIndex)
            .ToArray();

        // Create a new list of sortable items
        var sortableItems = new List<ISortableItem>();

        // Create the sortable items
        for (var i = 0; i < persistentEntries.Length; i++)
        {
            var entry = persistentEntries[i];
            var sortableItem = CreateSortableItem(_connection, LoadoutId, entry, i);
            sortableItems.Add(sortableItem);
        }

        return sortableItems;
    }

    /// <summary>
    /// In order to trigger the cache subscription, this method MUST be called within
    ///  the implementation's constructor.
    /// </summary>
    protected void Initialize()
    {
        // load the previously saved order
        var order = GenerateSortableItems();
        _orderCache.AddOrUpdate(order);

        Observable.Create<bool>(observer =>
                {
                    var subscription = GetItemsChangedObservable()
                        .Subscribe(_ => observer.OnNext(true))
                        .AddTo(_disposables);
                    return subscription;
                }
            )
            .SubscribeAwait(async (_, token) => { await UpdateOrderCache(token); })
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
    /// Changesets are unreliable for our purposes, so we need to update the order cache
    ///  from the persistent entries.
    /// </summary>
    /// <param name="token"></param>
    private async Task UpdateOrderCache(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var availableMods = GetPersistentEntries(_connection.Db);
            var currentOrder = _orderCache.Items.OrderBy(item => item.SortIndex);

            if (token.IsCancellationRequested) return;

            // Update the order
            var stagingList = SynchronizeSortingToItems(currentOrder.ToList(), availableMods);

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
    /// Persist the sortable items to the database and remove the cached sortable
    ///  items that are no longer available.
    ///  This should ideally be overriden by the implementation.
    /// </summary>
    /// <param name="orderList">The list of sortable items we want to persist</param>
    /// <param name="token"></param>
    protected virtual async Task PersistSortableItems(List<ISortableItem> orderList, CancellationToken token)
    {
        // See what we actually have in our cache.
        var cachedSortableItems = _orderCache.Items
            .OrderBy(item => item.SortIndex)
            .ToList();

        // Get all the persistent entries from the implementation.
        var persistentSortableItems = GetPersistentEntries(_connection.Db);

        if (token.IsCancellationRequested) return;

        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            if (!dbItem.SortableEntityId.HasValue)
            {
                // We can't deal with this item - it probably hasn't been added
                //  to the db yet.
                continue;
            }
            var liveItem = cachedSortableItems.FirstOrOptional(
                i => i.ItemId.Equals(dbItem.ItemId)
            );
            
            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem.SortableEntityId.Value, recursive: false);
                continue;
            }

            var liveIdx = orderList.IndexOf(liveItem.Value);

            if (dbItem.SortIndex != liveIdx)
            {
                tx.Add(dbItem.SortableEntityId.Value, SortableEntry.SortIndex, liveIdx);
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
    /// This method generates a new order list from currentOrder, after removing items that are no longer available and
    /// adding new items that have become available.
    /// New items are added at the beginning of the list, to make them win over existing items.
    /// </summary>
    /// <param name="currentOrder">The starting order</param>
    /// <param name="availableMods">Collection of mods to synchronize against</param>
    /// <returns>The new sorting</returns>
    protected List<ISortableItem> SynchronizeSortingToItems(
        List<ISortableItem> currentOrder,
        List<TItem> availableMods)
    {
        var modsToAdd = new List<TItem>();
        var sortableItemsToRemove = new List<ISortableItem>();

        // Find items to remove
        foreach (var si in currentOrder)
        {
            // TODO: determine the winning mod in case of multiple mods with the same name
            var modMatch = availableMods.FirstOrOptional(g => g.ItemId.Equals(si.ItemId));
            if (!modMatch.HasValue)
            {
                sortableItemsToRemove.Add(si);
            }
        }

        // Find items to add
        foreach (var mod in availableMods)
        {
            var sortableItem = currentOrder.FirstOrOptional(item =>
                item.ItemId.Equals(mod.ItemId)
            );

            if (!sortableItem.HasValue)
            {
                modsToAdd.Add(mod);
            }
        }

        // Get a staging list of the items to make changes to
        var stagingList = currentOrder
            .OrderBy(item => item.SortIndex)
            .ToList();

        stagingList.Remove(sortableItemsToRemove);

        // Insert based on the sort index behavior (i.e. smaller index wins or greater index wins)
        //  we want the items to be added as "winners" out of the box.
        var insertIdx = ParentFactory.IndexOverrideBehavior.Equals(IndexOverrideBehavior.GreaterIndexWins) ? stagingList.Capacity : 0;
        stagingList.InsertRange(insertIdx,
            modsToAdd.Select((obj, idx) => CreateSortableItem(_connection, LoadoutId, obj,
                    insertIdx + idx
                )
            )
        );

        for (var i = 0; i < stagingList.Count; i++)
        {
            var item = stagingList[i];
            item.SortIndex = i;
        }

        return stagingList;
    }

    /// <summary>
    /// Obtains the SortOrder model for this provider - default is cute, but the implementation
    ///  could generate its own derived SortOrder model if needed and pass it to the abstraction.
    /// </summary>
    protected static async ValueTask<SortOrder.ReadOnly> GetOrAddDefaultSortOrderModel(
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
    public void Dispose()
    {
        _disposables.Dispose();
        _semaphore.Dispose();
        _orderCache.Dispose();
    }
}
