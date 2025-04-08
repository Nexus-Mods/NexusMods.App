using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Extensions.BCL;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using R3;


namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

using RedModWithState = (RedModLoadoutGroup.ReadOnly RedMod, RelativePath RedModFolder, bool IsEnabled);

public class RedModSortableItemProvider : ILoadoutSortableItemProvider
{
    private readonly IConnection _connection;

    private readonly SourceCache<RedModSortableItem, string> _orderCache = new(item => item.RedModFolderName);

    private readonly ReadOnlyObservableCollection<ISortableItem> _readOnlyOrderList;

    private readonly SortOrderId _sortOrderId;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CompositeDisposable _disposables = new();
    public ReadOnlyObservableCollection<ISortableItem> SortableItems => _readOnlyOrderList;
    public IObservable<IChangeSet<ISortableItem, Guid>> SortableItemsChangeSet { get; }


    public LoadoutId LoadoutId { get; }
    public ISortableItemProviderFactory ParentFactory { get; }

    public static async Task<RedModSortableItemProvider> CreateAsync(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        var sortOrderModel = await GetOrAddSortOrderModel(connection, loadoutId, parentFactory);
        return new RedModSortableItemProvider(connection,
            loadoutId,
            sortOrderModel,
            parentFactory
        );
    }

    private RedModSortableItemProvider(
        IConnection connection,
        LoadoutId loadoutId,
        RedModSortOrder.ReadOnly sortOrderModel,
        ISortableItemProviderFactory parentFactory)
    {
        _connection = connection;
        LoadoutId = loadoutId;
        ParentFactory = parentFactory;
        _sortOrderId = sortOrderModel.AsSortOrder().SortOrderId;

        // load the previously saved order
        var order = RetrieveSortableEntries();
        _orderCache.AddOrUpdate(order);
        
        // populate read only list
        _orderCache.Connect()
            .Transform(item => item as ISortableItem)
            .SortBy(item => item.SortIndex)
            .Bind(out _readOnlyOrderList)
            .Subscribe()
            .AddTo(_disposables);
        
        SortableItemsChangeSet = SortableItems.ToObservableChangeSet(item => item.ItemId).RefCount();

        // Observe RedMod groups changes
        RedModLoadoutGroup.ObserveAll(_connection)
            .Transform((_, redModId) => LoadoutItem.Load(_connection.Db, redModId))
            // Filter by the loadout
            .Filter(item => item.LoadoutId.Equals(LoadoutId))
            .TransformOnObservable(item =>
                {
                    var redMod = RedModLoadoutGroup.Load(_connection.Db, item.Id);

                    // Observe this and all parents for changes on the `LoadoutItem.Disabled` attribute
                    return item.IsEnabledObservable(_connection)
                        .Select(isEnabled => (RedMod: redMod, RedModFolder: redMod.RedModFolder(), IsEnabled: isEnabled));
                }
            )
            .ToObservable()
            .SubscribeAwait(
                async (changes, token) => { await UpdateOrderCache(token); },
                awaitOperation: AwaitOperation.Sequential
            )
            .AddTo(_disposables);
    }


    public Optional<ISortableItem> GetSortableItem(Guid itemId)
    {
        return SortableItems.FirstOrOptional(item => item.ItemId.Equals(itemId));
    }

    public async Task SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var redModSortableItem = (RedModSortableItem)sortableItem;
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
            stagingList.Insert(newIndex, redModSortableItem);
            
            // Update the sort index of all items
            for (var i = 0; i < stagingList.Count; i++)
            {
                stagingList[i].SortIndex = i;
            }
            
            if (token.IsCancellationRequested) return;
            
            await PersistSortableEntries(stagingList, token);

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
    /// Returns the list of RedMod folder names, sorted by the load order, that are enabled in the loadout
    /// </summary>
    /// <param name="db">
    /// If provided, the method will attempt to retrieve both the RedMods and the sorting data from the specified database snapshot.
    /// If not provided, the method will use the latest available data in the database.
    /// In both cases, the method will perform the normal synchronization of the sorting to the available RedMods,
    /// but the order might not be the most up to date regardless.
    /// </param>
    public List<string> GetRedModOrder(IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;

        var redMods = RedModLoadoutGroup.All(dbToUse)
            .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
            .Select(g => (RedMod: g, RedModFolder: g.RedModFolder(), IsEnabled: g.IsEnabled()))
            .ToList();

        var enabledRedMods = redMods
            .Where(r => r.IsEnabled)
            .Select(r => r.RedModFolder)
            .ToList();

        // Retrieves the order from the database using the passed db
        // NOTE: depending on the db passed, the order might not be the latest
        var sortOrder = RetrieveSortableEntries(dbToUse);

        // Sanitize the order, applying it to the redMods in questions
        var validatedOrder = SynchronizeSortingToItems(redMods, sortOrder, this);

        return validatedOrder
            .Where(si => enabledRedMods.Any(m => m == si.RedModFolderName))
            .Select(si => si.RedModFolderName.ToString())
            .ToList();
    }

    private async Task UpdateOrderCache(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var redModsGroups = RedModLoadoutGroup.All(_connection.Db)
                .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
                .Select(g => new RedModWithState( g, g.RedModFolder(), g.IsEnabled()))
                .ToList();
                
            var oldOrder = _orderCache.Items.OrderBy(item => item.SortIndex);
            
            if (token.IsCancellationRequested) return;
            
            // Update the order
            var stagingList = SynchronizeSortingToItems(redModsGroups, oldOrder.ToList(), this);
            
            if (token.IsCancellationRequested) return;

            // Update the database
            await PersistSortableEntries(stagingList, token);
            
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

    private List<RedModSortableItem> RetrieveSortableEntries(IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;

        return RedModSortableEntry.All(dbToUse)
            .Where(si => si.IsValid() && si.AsSortableEntry().ParentSortOrderId == _sortOrderId)
            .OrderBy(si => si.AsSortableEntry().SortIndex)
            .Select(redModSortableItem =>
                {
                    var sortableItem = redModSortableItem.AsSortableEntry();
                    return new RedModSortableItem(this,
                        sortableItem.SortIndex,
                        redModSortableItem.RedModFolderName,
                        // Temp values, will get updated when we load the RedMods
                        modName: redModSortableItem.RedModFolderName,
                        isActive: false
                    );
                }
            )
            .ToList();
    }

    /// <summary>
    /// This method generates a new order list from currentOrder, after removing items that are no longer available and
    /// adding new items that have become available.
    /// New items are added at the beginning of the list, to make them win over existing items.
    /// </summary>
    /// <param name="availableRedMods">Collection of RedMods to synchronize against</param>
    /// <param name="currentOrder">The starting order</param>
    /// <returns>The new sorting</returns>
    private static List<RedModSortableItem> SynchronizeSortingToItems(
        IReadOnlyList<RedModWithState> availableRedMods,
        List<RedModSortableItem> currentOrder,
        RedModSortableItemProvider provider)
    {
        var redModsToAdd = new List<RedModWithState>();
        var sortableItemsToRemove = new List<RedModSortableItem>();

        // Find items to remove
        foreach (var si in currentOrder)
        {
            // TODO: determine the winning mod in case of multiple mods with the same name
            var redModMatch = availableRedMods.FirstOrOptional(
                g => g.RedModFolder == si.RedModFolderName
            );

            if (!redModMatch.HasValue)
            {
                sortableItemsToRemove.Add(si);
            }
        }

        // Find items to add
        foreach (var redMod in availableRedMods)
        {
            var redModFolder = redMod.RedModFolder;

            var sortableItem = currentOrder.FirstOrOptional(item => item.RedModFolderName == redModFolder);

            if (!sortableItem.HasValue)
            {
                redModsToAdd.Add(redMod);
            }
        }

        // Get a staging list of the items to make changes to
        var stagingList = currentOrder
            .OrderBy(item => item.SortIndex)
            .ToList();

        stagingList.Remove(sortableItemsToRemove);

        // New items should win over existing ones,
        // for RedMods this means they should be added at the beginning of the order.
        stagingList.InsertRange(0,
            redModsToAdd.Select((redMod, idx) =>
                new RedModSortableItem(provider,
                    idx,
                    redMod.RedModFolder.ToString(),
                    redMod.RedMod.AsLoadoutItemGroup().AsLoadoutItem().Parent.AsLoadoutItem().Name,
                    isActive: redMod.IsEnabled
                )
            )
        );

        for (var i = 0; i < stagingList.Count; i++)
        {
            var item = stagingList[i];
            item.SortIndex = i;

            // TODO: determine the winning mod in case of multiple mods with the same name, instead of just the first one
            if (!availableRedMods.TryGetFirst(g => g.RedModFolder == item.RedModFolderName, out var redModMatch))
            {
                // shouldn't happen because any missing items should have been added
                continue;
            }

            item.IsActive = redModMatch.IsEnabled;
            item.ModName = redModMatch.RedMod.AsLoadoutItemGroup().AsLoadoutItem().Parent.AsLoadoutItem().Name;
        }


        return stagingList;
    }


    private async Task PersistSortableEntries(List<RedModSortableItem> orderList, CancellationToken token)
    {
        var persistentSortableItems = RedModSortableEntry.All(_connection.Db)
            .Where(si => si.IsValid() && si.AsSortableEntry().ParentSortOrderId == _sortOrderId)
            .OrderBy(si => si.AsSortableEntry().SortIndex)
            .ToArray();

        if (token.IsCancellationRequested) return;
        
        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            var liveItem = orderList.FirstOrOptional(
                i => i.RedModFolderName == dbItem.RedModFolderName
            );

            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }

            var liveIdx = orderList.IndexOf(liveItem.Value);

            if (dbItem.AsSortableEntry().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < orderList.Count; i++)
        {
            var liveItem = orderList[i];
            if (persistentSortableItems.Any(si => si.RedModFolderName == liveItem.RedModFolderName))
                continue;

            var newDbItem = new SortableEntry.New(tx)
            {
                ParentSortOrderId = _sortOrderId,
                SortIndex = i,
            };

            _ = new RedModSortableEntry.New(tx, newDbItem)
            {
                SortableEntry = newDbItem,
                RedModFolderName = liveItem.RedModFolderName,
            };
        }

        if (token.IsCancellationRequested) return;

        await tx.Commit();
    }

    private static async ValueTask<RedModSortOrder.ReadOnly> GetOrAddSortOrderModel(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        var redModSortOrders = RedModSortOrder.All(connection.Db);
        var sortOrder = redModSortOrders
            .FirstOrOptional(lo => lo.AsSortOrder().LoadoutId == loadoutId);

        if (sortOrder.HasValue)
            return sortOrder.Value;

        using var ts = connection.BeginTransaction();
        var newSortOrder = new Abstractions.Loadouts.SortOrder.New(ts)
        {
            LoadoutId = loadoutId,
            SortOrderTypeId = parentFactory.SortOrderTypeId,
        };

        var newRedModSortOrder = new RedModSortOrder.New(ts, newSortOrder)
        {
            SortOrder = newSortOrder,
            IsMarker = true,
        };

        var commitResult = await ts.Commit();

        sortOrder = commitResult.Remap(newRedModSortOrder);
        return sortOrder.Value;
    }
    

    public void Dispose()
    {
        _disposables.Dispose();
        _semaphore.Dispose();
        _orderCache.Dispose();
    }
}
