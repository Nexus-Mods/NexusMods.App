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

public class RedModSortOrderVariety : ASortOrderVariety<
    SortItemKey<string>, 
    RedModSortableItem, 
    SortableItemLoadoutData<SortItemKey<string>>, 
    SortItemData<SortItemKey<string>> >
{
    private static readonly SortOrderVarietyId StaticVarietyId = SortOrderVarietyId.From(new Guid("9120C6F5-E0DD-4AD2-A99E-836F56796950"));
    
    public override SortOrderVarietyId SortOrderVarietyId => StaticVarietyId;

    public RedModSortOrderVariety(IServiceProvider serviceProvider, ISortOrderManager manager) : base(serviceProvider, manager) { }
    
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
            ParentEntity = parentEntity,
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
        // Make sure to use the correct db for the query
    }

    protected override void PersistSortOrderCore(
        SortOrderId sortOrderId,
        IReadOnlyList<SortItemData<SortItemKey<string>>> newOrder,
        ITransaction tx,
        IDb startingDb,
        CancellationToken token = default)
    {
        var persistentSortableEntries = startingDb.RetrieveRedModSortableEntries(sortOrderId);
        
        token.ThrowIfCancellationRequested();
        
        // Remove outdated persistent items
        foreach (var dbItem in persistentSortableEntries)
        {
            var newItem = newOrder.FirstOrOptional(
                newItem => newItem.Key.Key == dbItem.RedModFolderName
            );

            if (!newItem.HasValue)
            {
                tx.Delete(dbItem, recursive: false);
                continue;
            }

            var liveIdx = newOrder.IndexOf(newItem.Value);
            
            // Update existing items
            if (dbItem.AsSortableEntry().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortableEntry.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < newOrder.Count; i++)
        {
            var newItem = newOrder[i];
            if (persistentSortableEntries.Any(si => si.RedModFolderName == newItem.Key.Key))
                continue;

            var newDbItem = new SortableEntry.New(tx)
            {
                ParentSortOrderId = sortOrderId,
                SortIndex = i,
            };

            _ = new RedModSortableEntry.New(tx, newDbItem)
            {
                SortableEntry = newDbItem,
                RedModFolderName = newItem.Key.Key,
            };
        }

        token.ThrowIfCancellationRequested();
    }
    
    /// <inheritdoc />
    protected override IReadOnlyList<SortItemData<SortItemKey<string>>> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb dbToUse)
    {
        return dbToUse.RetrieveRedModSortableEntries(sortOrderEntityId)
            .Select(redModSortableEntry =>
                {
                    var sortableEntry = redModSortableEntry.AsSortableEntry();
                    return new SortItemData<SortItemKey<string>>(
                        new SortItemKey<string>(redModSortableEntry.RedModFolderName.Path),
                        sortableEntry.SortIndex
                    );
                }
            )
            .ToList();
    }
    
    /// <inheritdoc />
    protected override IReadOnlyList<SortableItemLoadoutData<SortItemKey<string>>> RetrieveLoadoutData(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId, IDb? db)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<(SortItemData<SortItemKey<string>> SortedEntry, SortableItemLoadoutData<SortItemKey<string>> ItemLoadoutData)> Reconcile(
        IReadOnlyList<SortItemData<SortItemKey<string>>> sourceSortedEntries, 
        IReadOnlyList<SortableItemLoadoutData<SortItemKey<string>>> loadoutDataItems)
    {
        var loadoutItemsDict = loadoutDataItems.ToDictionary(item => item.Key);
    
        // Start with a copy of source items
        var results = new List<(SortItemData<SortItemKey<string>> SortedEntry, SortableItemLoadoutData<SortItemKey<string>> ItemLoadoutData)>(sourceSortedEntries.Count);
        var processedKeys = new HashSet<SortItemKey<string>>(sourceSortedEntries.Count);


        foreach (var sortedEntry in sourceSortedEntries)
        {
            // No matching loadout data, skip this entry
            if (!loadoutItemsDict.TryGetValue(sortedEntry.Key, out var loadoutItemData))
                continue;
            
            processedKeys.Add(sortedEntry.Key);

            // Create sortable item from sorted entry and loadout data
            results.Add((sortedEntry, loadoutItemData));
        }
    
        // Add any remaining loadout items that were not in the source sorted entries
        var itemsToAdd = loadoutItemsDict.Values
            .Where(item => !processedKeys.Contains(item.Key))
            .OrderByDescending(item => item.ModGroupId)
            .Select(loadoutItemData => (
                new SortItemData<SortItemKey<string>>(loadoutItemData.Key, 0), // SortIndex will be updated later
                loadoutItemData
            ));

        // Insert new items at the start, sorted by newest creation order (ModGroupId)
        // For cyberpunk RedMods, lower index wins, so we add new items at the start
        results.InsertRange(0, itemsToAdd);
    
        // Update sort indices
        for (var i = 0; i < results.Count; i++)
        {
            results[i].SortedEntry.SortIndex = i;
        }
    
        return results;
    }
}
