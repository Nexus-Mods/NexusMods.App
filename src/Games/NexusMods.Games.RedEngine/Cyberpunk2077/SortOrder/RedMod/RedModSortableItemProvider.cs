using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Interfaces;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModWithState(
    RedModLoadoutGroup.ReadOnly redMod,
    RelativePath redModFolder,
    bool isEnabled,
    Guid itemId,
    EntityId entityId) : ISortableDbEntryConstraints
{
    public RedModLoadoutGroup.ReadOnly RedMod { get; } = redMod;
    public RelativePath RedModFolder { get; } = redModFolder;
    public bool IsEnabled { get; } = isEnabled;
    public Guid ItemId { get; } = itemId;
    public EntityId EntityId { get; } = entityId;
    public int SortIndex { get; set; }
}

public class RedModSortableItemProvider : ASortableItemProvider<RedModWithState>
{
    private readonly IConnection _connection;
    
    protected override ISortableItem CreateSortableItem(IConnection connection, LoadoutId loadoutId, RedModWithState item, int idx)
    {
        var folderName = item.RedModFolder;
        return new RedModSortableItem(this,
            sortIndex: idx,
            redModFolderName: folderName,
            // Temp values, will get updated when we load the RedMods
            modName: folderName,
            isActive: false,
            itemId: item.ItemId
        );
    }

    protected override List<RedModWithState> GetPersistentEntries(IDb? db = null)
    {
        var dbToUse = db ?? _connection.Db;
        return dbToUse.GetRedModsWithState(LoadoutId, SortOrderId);
    }

    public static async Task<RedModSortableItemProvider> CreateAsync(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory)
    {
        // Get the sort order model - this should ideally be overriden by the implementation.
        var sortOrderModel = await GetOrAddSortOrderModel(connection, loadoutId, parentFactory);
        return new RedModSortableItemProvider(connection,
            sortOrderModel,
            loadoutId,
            parentFactory
        );
    }

    private RedModSortableItemProvider(
        IConnection connection,
        RedModSortOrder.ReadOnly sortOrderModel,
        LoadoutId loadoutId,
        ISortableItemProviderFactory parentFactory) : base(connection, sortOrderModel.AsSortOrder(), loadoutId, parentFactory)
    {
        _connection = connection;
        Initialize();
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

        var redMods = GetPersistentEntries(dbToUse);
        var redModsWithState = dbToUse.GetRedModsWithState(LoadoutId, SortOrderId);

        var enabledRedMods = redMods
            .Where(r => redModsWithState.Any(m => r.ItemId == m.ItemId && m.IsEnabled))
            .ToList();

        // Retrieves the order from the database using the passed db
        // NOTE: depending on the db passed, the order might not be the latest
        var sortOrder = GetSortableEntries(dbToUse);

        // Sanitize the order, applying it to the redMods in questions
        var validatedOrder = SynchronizeSortingToItems(sortOrder, enabledRedMods);

        return validatedOrder
            .Where(si => enabledRedMods.Any(m => m.ItemId.Equals(si.ItemId)))
            .Select(si => si.ModName)
            .ToList();
    }

    protected override IObservable<bool> GetObservableChanges()
    {
        return RedModLoadoutGroup.ObserveAll(_connection)
            .Transform((_, redModId) => LoadoutItem.Load(_connection.Db, redModId))
            .Filter(item => item.LoadoutId.Equals(LoadoutId))
            .TransformOnObservable(item =>
                {
                    var redMod = RedModLoadoutGroup.Load(_connection.Db, item.Id);
                    return item.IsEnabledObservable(_connection);
                }
            )
            .Select(change => true);
    }

    protected override async Task PersistSortableItems(List<ISortableItem> orderList, CancellationToken token)
    {
        var db = _connection.Db;
        var persistentEntries = GetPersistentEntries(db);

        if (token.IsCancellationRequested) return;
        
        using var tx = _connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentEntries)
        {
            var liveItem = orderList.FirstOrOptional(
                i => i.ItemId.Equals(dbItem.ItemId)
            );

            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem.EntityId, recursive: false);
                continue;
            }

            var liveIdx = orderList.IndexOf(liveItem.Value);

            if (dbItem.SortIndex != liveIdx)
            {
                tx.Add(dbItem.EntityId, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items.
        for (var i = 0; i < orderList.Count; i++)
        {
            var liveItem = orderList[i];
            if (persistentEntries.Any(si => si.ItemId.Equals(liveItem.ItemId)))
                continue;

            var newDbItem = new SortableEntry.New(tx)
            {
                ItemId = liveItem.ItemId,
                ParentSortOrderId = SortOrderId,
                SortIndex = i,
            };

            _ = new RedModSortableEntry.New(tx, newDbItem)
            {
                SortableEntry = newDbItem,
                RedModFolderName = liveItem.ModName,
            };
        }

        if (token.IsCancellationRequested) return;

        await tx.Commit();
    }

    private static async ValueTask<RedModSortOrder.ReadOnly> GetOrAddSortOrderModel(
        IConnection connection,
        LoadoutId loadoutId,
        ISortableItemProviderFactory factory)
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
            SortOrderTypeId = factory.SortOrderTypeId,
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
}
