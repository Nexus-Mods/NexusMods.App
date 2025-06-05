using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using OneOf;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortOrderVariety : ASortOrderVariety<RedModSortableItem, SortItemKey<string>>
{
    private static readonly SortOrderVarietyId StaticVarietyId = SortOrderVarietyId.From(new Guid("9120C6F5-E0DD-4AD2-A99E-836F56796950"));
    
    public override SortOrderVarietyId SortOrderVarietyId => StaticVarietyId;

    public RedModSortOrderVariety(IServiceProvider serviceProvider, ISortOrderManager manager) : base(serviceProvider, manager)
    {
        
    }
    
    public override SortOrderUiMetadata SortOrderUiMetadata { get; } = new()
    {
        SortOrderName = "REDmod Load Order",
        OverrideInfoTitle = "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins",
        OverrideInfoMessage = """
                               Some Cyberpunk 2077 mods use REDmods modules to alter core gameplay elements. If two REDmods modify the same part of the game, the one loaded first will take priority and overwrite changes from those loaded later.
                               For example, the 1st position overwrites the 2nd, the 2nd overwrites the 3rd, and so on.
                               """,
        WinnerIndexToolTip = "First Loaded RedMOD Wins: Items that load first will overwrite changes from items loaded after them.",
        IndexColumnHeader = "Load Order",
        DisplayNameColumnHeader = "REDmod Name",
        EmptyStateMessageTitle = "No REDmods detected",
        EmptyStateMessageContents = "Some mods contain REDmod items that alter core gameplay elements. When detected, they will appear here for load order configuration.",
        LearnMoreUrl = "https://nexus-mods.github.io/NexusMods.App/users/games/Cyberpunk2077/#redmod-load-ordering"
    };

    public override async ValueTask<SortOrderId> GetOrCreateSortOrderFor(
        LoadoutId loadoutId, 
        OneOf<LoadoutId, CollectionGroupId> parentEntity,
        CancellationToken token = default)
    {
        var optionalSortOrderId = GetSortOrderIdFor(parentEntity);
        if (optionalSortOrderId.HasValue)
            return optionalSortOrderId.Value;
        
        token.ThrowIfCancellationRequested();
        
        using var ts = Connection.BeginTransaction();
        var newSortOrder = new Abstractions.Loadouts.SortOrder.New(ts)
        {
            LoadoutId = loadoutId,
            // TODO: update to use the collection group id if dealing with a collection sort order
            ParentEntity = loadoutId,
            SortOrderTypeId = SortOrderVarietyId.Value,
        };

        var newRedModSortOrder = new RedModSortOrder.New(ts, newSortOrder)
        {
            SortOrder = newSortOrder,
            IsMarker = true,
        };

        var commitResult = await ts.Commit();

        var redModSortOrder = commitResult.Remap(newRedModSortOrder);
        return redModSortOrder.AsSortOrder().SortOrderId;
    }

    public override IObservable<IChangeSet<RedModSortableItem, SortItemKey<string>>> GetSortableItemsChangeSet(SortOrderId sortOrderId)
    {
        throw new NotImplementedException();
    }

    public override IReadOnlyList<RedModSortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<SortItemKey<string>> items, IDb? db = null, CancellationToken token = default)
    {
        var dbToUse = db ?? Connection.Db;

        var keyOrderList = items.ToArray();
        
        var persistentSortableItems = dbToUse.RetrieveRedModSortableEntries(sortOrderId);
        
        if (token.IsCancellationRequested) return;
        
        using var tx = Connection.BeginTransaction();

        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableItems)
        {
            var liveItem = keyOrderList.FirstOrOptional(
                i => i.Key == dbItem.RedModFolderName
            );

            if (!liveItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }

            var liveIdx = keyOrderList.IndexOf(liveItem.Value);

            if (dbItem.AsSortableEntry().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < keyOrderList.Length; i++)
        {
            var liveItem = keyOrderList[i];
            if (persistentSortableItems.Any(si => si.RedModFolderName == liveItem.Key))
                continue;

            var newDbItem = new SortableEntry.New(tx)
            {
                ParentSortOrderId = sortOrderId,
                SortIndex = i,
            };

            _ = new RedModSortableEntry.New(tx, newDbItem)
            {
                SortableEntry = newDbItem,
                RedModFolderName = liveItem.Key,
            };
        }

        if (token.IsCancellationRequested) return;

        await tx.Commit();
    }

    public override ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    protected override IReadOnlyList<RedModSortableItem> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb? db = null)
    {
        throw new NotImplementedException();
        
        var dbToUse = db ?? Connection.Db;
        
        // TODO: This should also retrieve the data from the loadout and attach it to the sortableItems
    }
    
    /// <inheritdoc />
    protected override async Task PersistSortOrder(IReadOnlyList<RedModSortableItem> items, SortOrderId sortOrderEntityId, CancellationToken token)
    {
        var redModOrderList = items.Select(item => item.Key).ToList();

        await SetSortOrder(sortOrderEntityId, redModOrderList, token: token);
    }
}
