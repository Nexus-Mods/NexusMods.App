using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Extensions.BCL;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.LoadOrder;

public class RedModSortableItemProvider : ILoadoutSortableItemProvider, IDisposable
{
    private readonly IConnection _connection;

    private readonly SourceCache<RedModSortableItem, string> _orderCache = new(item => item.DisplayName);
    private readonly ReadOnlyObservableCollection<ISortableItem> _readOnlyOrderList;
    private readonly LoadOrderId _loadOrderId;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CompositeDisposable _disposables = new();

    public ReadOnlyObservableCollection<ISortableItem> SortableItems => _readOnlyOrderList;


    public LoadoutId LoadoutId { get; }
    public ISortableItemProviderFactory ParentFactory { get; }

    public static async Task<RedModSortableItemProvider> CreateAsync(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        var loadOrder = await GetOrAddLoadOrderModel(connection, loadoutId, parentFactory);
        return new RedModSortableItemProvider(connection,
            loadoutId,
            loadOrder,
            parentFactory
        );
    }

    private RedModSortableItemProvider(
        IConnection connection,
        LoadoutId loadoutId,
        RedModLoadOrder.ReadOnly loadOrder,
        ISortableItemProviderFactory parentFactory)
    {
        _connection = connection;
        LoadoutId = loadoutId;
        ParentFactory = parentFactory;
        _loadOrderId = loadOrder.AsLoadOrder().LoadOrderId;

        // load the previously saved order
        var order = RedModSortableItemModel.All(_connection.Db)
            .Where(si => si.IsValid() && si.AsSortableItemModel().ParentLoadOrderId == _loadOrderId)
            .OrderBy(si => si.AsSortableItemModel().SortIndex)
            .Select(redModSortableItem =>
                {
                    var sortableItem = redModSortableItem.AsSortableItemModel();
                    return new RedModSortableItem(this,
                        sortableItem.SortIndex,
                        sortableItem.Name,
                        redModSortableItem.RedModFolderName,
                        isActive: false // Will need to be updated when we load the RedMods
                    );
                }
            );
        _orderCache.AddOrUpdate(order);

        _orderCache.Connect()
            .Transform(item => item as ISortableItem)
            .SortBy(item => item.SortIndex)
            .Bind(out _readOnlyOrderList)
            .Subscribe()
            .AddTo(_disposables);


        // Observe changes in the RedMods and adjust the order list accordingly
        RedModLoadoutGroup.ObserveAll(_connection)
            .Filter(group => group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
            .SortBy(g => RedModFolder(g).ToString())
            .Bind(out var redModsGroups)
            .ToObservable()
            .SubscribeAwait(async (changes, _) =>
                {
                    var redModsToAdd = new List<RedModLoadoutGroup.ReadOnly>();
                    var sortableItemsToRemove = new List<RedModSortableItem>();

                    // Find items to remove
                    foreach (var si in _orderCache.Items)
                    {
                        // TODO: determine the winning mod in case of multiple mods with the same name
                        var redModMatch = redModsGroups.FirstOrOptional(
                            g => RedModFolder(g).ToString() == si.DisplayName
                        );

                        if (!redModMatch.HasValue)
                        {
                            sortableItemsToRemove.Add(si);
                        }
                    }

                    // Find items to add
                    foreach (var redMod in redModsGroups)
                    {
                        var redModFolder = RedModFolder(redMod);

                        var sortableItem = _orderCache.Lookup(redModFolder);

                        if (!sortableItem.HasValue)
                        {
                            redModsToAdd.Add(redMod);
                        }
                    }

                    // Get a staging list of the items to make changes to
                    var stagingList = _orderCache.Items
                        .OrderBy(item => item.SortIndex)
                        .ToList();

                    stagingList.Remove(sortableItemsToRemove);

                    // New items should win over existing ones,
                    // for RedMods this means they should be added at the beginning of the order.
                    stagingList.InsertRange(0,
                        redModsToAdd.Select((redMod, idx) =>
                            new RedModSortableItem(this,
                                idx,
                                RedModFolder(redMod).ToString(),
                                redMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                                isActive: RedModIsEnabled(redMod)
                            )
                        )
                    );

                    for (var i = 0; i < stagingList.Count; i++)
                    {
                        var item = stagingList[i];
                        item.SortIndex = i;

                        if (!redModsGroups.TryGetFirst(g => RedModFolder(g).ToString() == item.DisplayName, out var redModMatch))
                        {
                            // shouldn't happen because any missing items should have been added
                            continue;
                        }

                        item.IsActive = RedModIsEnabled(redModMatch);
                        item.ModName = redModMatch.AsLoadoutItemGroup().AsLoadoutItem().Name;
                    }
                    
                    // Update the database
                    await PersistOrder(stagingList);
                    
                    // Update the cache
                    _orderCache.Edit(innerCache =>
                        {
                            innerCache.Clear();
                            innerCache.AddOrUpdate(stagingList);
                        }
                    );
                },
                awaitOperation: AwaitOperation.Sequential
            )
            .AddTo(_disposables);
    }


    private async Task PersistOrder(List<RedModSortableItem> orderList)
    {
        await _semaphore.WaitAsync();
        var persistentSortableItems = RedModSortableItemModel.All(_connection.Db)
            .Where(si => si.IsValid() && si.AsSortableItemModel().ParentLoadOrderId == _loadOrderId)
            .OrderBy(si => si.AsSortableItemModel().SortIndex)
            .ToArray();

        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            var liveItem = orderList.FirstOrOptional(
                i => i.DisplayName == dbItem.RedModFolderName
            );

            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }

            var liveIdx = orderList.IndexOf(liveItem.Value);

            if (dbItem.AsSortableItemModel().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableItemModel.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < orderList.Count; i++)
        {
            var liveItem = orderList[i];
            if (persistentSortableItems.Any(si => si.RedModFolderName == liveItem.DisplayName))
                continue;

            var newDbItem = new SortableItemModel.New(tx)
            {
                ParentLoadOrderId = _loadOrderId,
                SortIndex = i,
                Name = liveItem.DisplayName,
            };

            _ = new RedModSortableItemModel.New(tx, newDbItem)
            {
                SortableItemModel = newDbItem,
                RedModFolderName = liveItem.DisplayName,
            };
        }

        await tx.Commit();
        _semaphore.Release();
    }

    private static async ValueTask<RedModLoadOrder.ReadOnly> GetOrAddLoadOrderModel(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        var loadOrder = RedModLoadOrder.All(connection.Db)
            .FirstOrOptional(lo => lo.AsLoadOrder().LoadoutId == loadoutId);

        if (loadOrder.HasValue)
            return loadOrder.Value;

        using var ts = connection.BeginTransaction();
        var newLoadOrder = new Abstractions.Loadouts.LoadOrder.New(ts)
        {
            LoadoutId = loadoutId,
            LoadOrderTypeId = parentFactory.StaticLoadOrderTypeId,
        };

        var newRedModLoadOrder = new RedModLoadOrder.New(ts, newLoadOrder.LoadOrderId)
        {
            LoadOrder = newLoadOrder,
            Revision = 0,
        };

        var commitResult = await ts.Commit();

        loadOrder = commitResult.Remap(newRedModLoadOrder);
        return loadOrder.Value;
    }

    
    public async Task SetRelativePosition(ISortableItem sortableItem, int delta)
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
        if (newIndex < 0 || newIndex >= stagingList.Count)
            return;

        // Move the item in the list
        stagingList.RemoveAt(currentIndex);
        stagingList.Insert(newIndex, redModSortableItem);

        // Update the sort index of all items
        for (var i = 0; i < stagingList.Count; i++)
        {
            stagingList[i].SortIndex = i;
        }
        
        await PersistOrder(stagingList);
        
        _orderCache.Edit(innerCache =>
            {
                innerCache.Clear();
                innerCache.AddOrUpdate(stagingList);
            }
        );
    }

    public List<string> GetRedModOrder(IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;
        
        var enabledRedMods = RedModLoadoutGroup.All(dbToUse)
            .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
            .Where(RedModIsEnabled)
            .Select(g => RedModFolder(g).ToString())
            .ToList();
        
        return RedModSortableItemModel.All(dbToUse)
            .Where(si => si.IsValid() && si.AsSortableItemModel().ParentLoadOrderId == _loadOrderId)
            .Where(si => enabledRedMods.Any(m => m == si.RedModFolderName))
            .OrderBy(si => si.AsSortableItemModel().SortIndex)
            .Select(si => si.RedModFolderName)
            .ToList();
    }


    private static bool RedModIsEnabled(RedModLoadoutGroup.ReadOnly grp)
    {
        return !grp.AsLoadoutItemGroup().AsLoadoutItem().GetThisAndParents().Any(f => f.Contains(LoadoutItem.Disabled));
    }

    private static RelativePath RedModFolder(RedModLoadoutGroup.ReadOnly group)
    {
        var redModInfoFile = group.RedModInfoFile.AsLoadoutFile().AsLoadoutItemWithTargetPath().TargetPath.Item3;
        return redModInfoFile.Parent.FileName;
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _semaphore.Dispose();
        _orderCache.Dispose();
    }
}
