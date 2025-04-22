using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Extensions.BCL;
using NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using R3;
using ReactiveUI;


namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

using RedModWithState = (RedModLoadoutGroup.ReadOnly RedMod, RelativePath RedModFolder, bool IsEnabled);

public class RedModSortableItemProvider : ASortableItemProvider
{
    private readonly IConnection _connection;
    private bool _isDisposed;

    private readonly ReadOnlyObservableCollection<ISortableItem> _readOnlyOrderList;

    private readonly CompositeDisposable _disposables = new();
    public override ReadOnlyObservableCollection<ISortableItem> SortableItems => _readOnlyOrderList;

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
        
        // populate read only list
        OrderCache.Connect()
            .Transform(item => item)
            .SortBy(item => item.SortIndex)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _readOnlyOrderList)
            .Subscribe()
            .AddTo(_disposables);

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

    private async Task UpdateOrderCache(CancellationToken token)
    {
        await Semaphore.WaitAsync(token);
        try
        {
            var redModsGroups = RedModLoadoutGroup.All(_connection.Db)
                .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
                .Select(g => new RedModWithState( g, g.RedModFolder(), g.IsEnabled()))
                .ToList();
                
            var oldOrder = OrderCache.Items.OfType<RedModSortableItem>().OrderBy(item => item.SortIndex);
            
            if (token.IsCancellationRequested) return;
            
            // Update the order
            var stagingList = SynchronizeSortingToItems(redModsGroups, oldOrder.ToList(), this);
            
            if (token.IsCancellationRequested) return;

            // Update the database
            await PersistSortOrder(stagingList, SortOrderEntityId, token);
            
            if (token.IsCancellationRequested) return;

            // Update the cache
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
        IReadOnlyList<ISortableItem> currentOrder,
        RedModSortableItemProvider provider)
    {
        var redModCurrentOrder = (IReadOnlyList<RedModSortableItem>)currentOrder;
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
    
    protected override async Task PersistSortOrder(IReadOnlyList<ISortableItem> orderList, SortOrderId sortOrderEntityId, CancellationToken token)
    {
        var redModOrderList = (IReadOnlyList<RedModSortableItem>)orderList;
        
        var persistentSortableItems = _connection.Db.RetrieveRedModSortOrder(sortOrderEntityId);

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


    /// <summary>
    /// Retrieves the sortable entries for the sortOrderId, and returns them as a list of sortable items.
    /// </summary>
    /// <remarks>
    /// The items in the returned list can have temporary values for properties such as `ModName` and `IsActive`.
    /// Those will need to be updated after the sortableItems are matched to items in the loadout. 
    /// </remarks>
    private IReadOnlyList<ISortableItem> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;

        return dbToUse.RetrieveRedModSortOrder(sortOrderEntityId)
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
