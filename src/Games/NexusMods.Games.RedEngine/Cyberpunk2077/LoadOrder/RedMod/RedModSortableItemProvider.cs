using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.LoadOrder;

public class RedModSortableItemProvider : ILoadoutSortableItemProvider
{
    private readonly IConnection _connection;
    private readonly ObservableList<ISortableItem> _orderList;
    private readonly LoadOrderId _loadOrderId;

    public ObservableList<ISortableItem> SortableItems => _orderList;


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
        _orderList = new ObservableList<ISortableItem>(order);


        // Observe changes in the RedMods and adjust the order list accordingly
        // TODO: try .StartEmpty() in case this doesn't refresh the first ones 
        RedModLoadoutGroup.ObserveAll(_connection)
            .Filter(group => group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == LoadoutId)
            .SortBy(g => RedModFolder(g).ToString())
            .Bind(out var redModsGroups)
            .ToObservable()
            .SubscribeAwait(async (change, cancellationToken) =>
                {
                    var redModsToAdd = new List<RedModLoadoutGroup.ReadOnly>();
                    var sortableItemsToRemove = new List<ISortableItem>();

                    // Find items to add
                    foreach (var redMod in redModsGroups)
                    {
                        var redModFolder = RedModFolder(redMod);
                        var redModIsEnabled = RedModIsEnabled(redMod);

                        var sortableItem = _orderList.FirstOrOptional(
                            i => i.DisplayName == redModFolder
                        );

                        if (sortableItem.HasValue)
                        {
                            sortableItem.Value.IsActive = redModIsEnabled;
                            // TODO: check if this mod is the winning one in case of multiple mods with the same name
                            sortableItem.Value.ModName = redMod.AsLoadoutItemGroup().AsLoadoutItem().Name;
                        }
                        else
                        {
                            redModsToAdd.Add(redMod);
                        }
                    }

                    // Find items to remove
                    foreach (var si in _orderList)
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

                    // Update the order list
                    _orderList.Remove(sortableItemsToRemove);
                    // New items should win over existing ones,
                    // for RedMods this means they should be added at the beginning of the order.
                    _orderList.InsertRange(0, redModsToAdd.Select((redMod, idx) =>
                            new RedModSortableItem(this,
                                idx,
                                RedModFolder(redMod).ToString(),
                                redMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                                isActive: RedModIsEnabled(redMod)
                            )
                        )
                    );
                    
                    foreach (var (s, idx) in _orderList.Select((s, idx) => (s, idx)))
                    {
                        s.SortIndex = idx;
                        
                        // TODO: determine the winning mod in case of multiple mods with the same name
                        var redModMatch = redModsGroups.FirstOrOptional(
                            g => RedModFolder(g).ToString() == s.DisplayName
                        );
                        
                        if (!redModMatch.HasValue)
                        {
                            // Shouldn't happen
                            continue;
                        }
                        
                        s.IsActive = RedModIsEnabled(redModMatch.Value);
                        s.ModName = redModMatch.Value.AsLoadoutItemGroup().AsLoadoutItem().Name;
                    }
                }
            );
        

        // Observe changes in the order list and update the database accordingly
        _orderList.ObserveChanged()
            .Select(_ => _orderList)
            .Prepend(_orderList)
            .SubscribeAwait(async (_, _) =>
                {
                    var persistentSortableItems = RedModSortableItemModel.All(_connection.Db)
                        .Where(si => si.IsValid() && si.AsSortableItemModel().ParentLoadOrderId == _loadOrderId)
                        .OrderBy(si => si.AsSortableItemModel().SortIndex).ToArray();
                    
                    using var tx = _connection.BeginTransaction();
                    
                    // Remove outdated persistent items
                    foreach (var dbItem in persistentSortableItems)
                    {
                        var liveItem = _orderList.FirstOrOptional(
                            i => i.DisplayName == dbItem.RedModFolderName
                        );
                        
                        if (!liveItem.HasValue)
                        {
                            tx.Delete(dbItem, recursive: false);
                            continue;
                        }
                        
                        var liveIdx = _orderList.IndexOf(liveItem.Value);
                        
                        if (dbItem.AsSortableItemModel().SortIndex != liveIdx)
                        {
                            tx.Add(dbItem, SortableItemModel.SortIndex, liveIdx);
                        }
                    }
                    
                    // Add new items
                    for (var i = 0; i < _orderList.Count; i++)
                    {
                        var liveItem = _orderList[i];
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
                },
                awaitOperation: AwaitOperation.ThrottleFirstLast
            );
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

    public Task SetRelativePosition(ISortableItem sortableItem, int delta)
    {
        var redModSortableItem = (RedModSortableItem)sortableItem;
        // Get the current index of the item relative to the full list
        var currentIndex = _orderList.IndexOf(sortableItem);

        // Get the new index of the group relative to the full list
        var newIndex = currentIndex + delta;

        // Ensure the new index is within the bounds of the list
        if (newIndex < 0 || newIndex >= _orderList.Count)
            return Task.CompletedTask;
        
        // Move the item in the list
        _orderList.Move(currentIndex, newIndex);
        
        // Update the sort index of all items
        for (var i = 0; i < _orderList.Count; i++)
        {
            _orderList[i].SortIndex = i;
        }
        
        return Task.CompletedTask;
    }

    public string[] GetRedModOrder()
    {
        return RedModSortableItemModel.All(_connection.Db)
            .Where(si => si.IsValid() && si.AsSortableItemModel().ParentLoadOrderId == _loadOrderId)
            .OrderBy(si => si.AsSortableItemModel().SortIndex)
            .Select(si => si.RedModFolderName)
            .ToArray();
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
}

