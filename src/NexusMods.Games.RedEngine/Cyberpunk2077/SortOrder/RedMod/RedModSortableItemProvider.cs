using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Sdk;
using R3;


namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

using RedModWithState = (RedModLoadoutGroup.ReadOnly RedMod, RelativePath RedModFolder, bool IsEnabled);

public class RedModSortableItemProvider : ASortableItemProvider<RedModSortableItem, SortItemKey<string>>
{
    private readonly IConnection _connection;
    private bool _isDisposed;

    private readonly CompositeDisposable _disposables = new();

    public static async Task<RedModSortableItemProvider> CreateAsync(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        var sortOrderModel = await GetOrAddRedModSortOrderModel(connection, loadoutId, parentFactory);
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
        ISortableItemProviderFactory parentFactory) :
        base(parentFactory, loadoutId, sortOrderModel.AsSortOrder().SortOrderId)
    {
        _connection = connection;

        // load the previously saved order
        var order = RetrieveSortOrder(SortOrderEntityId);
        OrderCache.AddOrUpdate(order);
        
        // Observe RedMod groups changes
        GetRedModChangesObservable()
            .SubscribeAwait(
                async (_, token) => { await RefreshSortOrder(token: token); },
                awaitOperation: AwaitOperation.Sequential
            )
            .AddTo(_disposables);
    }
    
    private Observable<Unit> GetRedModChangesObservable()
    {
        return RedModLoadoutGroup.ObserveAll(_connection)
            .Transform((_, redModId) => LoadoutItem.Load(_connection.Db, redModId))
            // filter by the loadout
            .Filter(item => item.LoadoutId.Equals(LoadoutId))
            // get the enabled state considering all parents as well
            .TransformOnObservable(item =>
                {
                    var redMod = RedModLoadoutGroup.Load(_connection.Db, item.Id);

                    // Observe this and all parents for changes on the `LoadoutItem.Disabled` attribute
                    return item.IsEnabledObservable(_connection)
                        .Select(isEnabled => (RedMod: redMod, RedModFolder: redMod.RedModFolder(), IsEnabled: isEnabled));
                }
            ).ToObservable()
            .Select(_ => Unit.Default);
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
    public List<string> GetRedModOrder(SortOrderId? sortOrderEntityId = null,  IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;
        var sortOrderId = sortOrderEntityId ?? SortOrderEntityId;

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
        var sortOrder = RetrieveSortOrder(sortOrderId, dbToUse);

        // Sanitize the order, applying it to the redMods in questions
        var validatedOrder = SynchronizeSortingToItems(redMods, sortOrder, this);

        return validatedOrder
            .Where(si => enabledRedMods.Any(m => m == si.RedModFolderName))
            .Select(si => si.RedModFolderName.ToString())
            .ToList();
    }

    public override async Task<IReadOnlyList<RedModSortableItem>> RefreshSortOrder(CancellationToken token, IDb? loadoutDb = null)
    {
        var hasEntered = await Semaphore.WaitAsync(SemaphoreTimeout, token);
        if (!hasEntered) throw new TimeoutException($"Timed out waiting for semaphore in RefreshSortOrder");
        
        try
        {
            var dbToUse = loadoutDb ?? _connection.Db;
            
            var redModsGroups = RedModLoadoutGroup.All(dbToUse)
                .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
                .Select(g => new RedModWithState(g, g.RedModFolder(), g.IsEnabled()))
                .ToList();
                
            var oldOrder = OrderCache.Items.OrderBy(item => item.SortIndex).ToList();
            
            if (token.IsCancellationRequested) return [];
            
            // Update the order
            var stagingList = SynchronizeSortingToItems(redModsGroups, oldOrder, this);
            
            if (token.IsCancellationRequested) return [];

            // Update the database
            await PersistSortOrder(stagingList, SortOrderEntityId, token);
            
            if (token.IsCancellationRequested) return [];

            // Update the cache
            OrderCache.Edit(innerCache =>
                {
                    innerCache.Clear();
                    innerCache.AddOrUpdate(stagingList);
                }
            );
            
            return stagingList;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// This method generates a new order list from currentOrder, after removing items that are no longer available and
    /// adding new items that have become available.
    /// New items are added at the beginning of the list, to make them win over existing items.
    /// </summary>
    /// <param name="availableRedMods">Collection of RedMods to synchronize against</param>
    /// <param name="currentOrder">The starting order</param>
    /// <param name="provider"></param>
    /// <returns>The new sorting</returns>
    private static IReadOnlyList<RedModSortableItem> SynchronizeSortingToItems(
        IReadOnlyList<RedModWithState> availableRedMods,
        IReadOnlyList<RedModSortableItem> currentOrder,
        RedModSortableItemProvider provider)
    {
        var redModCurrentOrder =  currentOrder.ToList();
        var redModsToAdd = new List<RedModWithState>();
        var sortableItemsToRemove = new List<RedModSortableItem>();

        // Find items to remove
        foreach (var si in redModCurrentOrder)
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

            var sortableItem = redModCurrentOrder.FirstOrOptional(item => item.RedModFolderName == redModFolder);

            if (!sortableItem.HasValue)
            {
                redModsToAdd.Add(redMod);
            }
        }

        // Get a staging list of the items to make changes to
        var stagingList = redModCurrentOrder
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
        
        // Update the data on the items
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
            item.ModGroupId = redModMatch.RedMod.AsLoadoutItemGroup().AsLoadoutItem().Parent.LoadoutItemGroupId;
        }

        return stagingList;
    }

    /// <inheritdoc />
    protected override async Task PersistSortOrder(IReadOnlyList<RedModSortableItem> orderList, SortOrderId sortOrderEntityId, CancellationToken token)
    {
        var redModOrderList = orderList.ToList();
        
        var persistentSortableItems = _connection.Db.RetrieveRedModSortableEntries(sortOrderEntityId);

        if (token.IsCancellationRequested) return;
        
        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            var liveItem = redModOrderList.FirstOrOptional(
                i => i.RedModFolderName == dbItem.RedModFolderName
            );

            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }

            var liveIdx = redModOrderList.IndexOf(liveItem.Value);

            if (dbItem.AsSortableEntry().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < redModOrderList.Count; i++)
        {
            var liveItem = redModOrderList[i];
            if (persistentSortableItems.Any(si => si.RedModFolderName == liveItem.RedModFolderName))
                continue;

            var newDbItem = new SortableEntry.New(tx)
            {
                ParentSortOrderId = sortOrderEntityId,
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

    /// <inheritdoc />
    protected sealed override IReadOnlyList<RedModSortableItem> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;

        return dbToUse.RetrieveRedModSortableEntries(sortOrderEntityId)
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

    private static async ValueTask<RedModSortOrder.ReadOnly> GetOrAddRedModSortOrderModel(
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
            // TODO: update to use the collection group id if dealing with a collection sort order
            ParentEntity = loadoutId,
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
    

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _disposables.Dispose();
        }

        _isDisposed = true;
        
        base.Dispose(disposing);
    }

}
