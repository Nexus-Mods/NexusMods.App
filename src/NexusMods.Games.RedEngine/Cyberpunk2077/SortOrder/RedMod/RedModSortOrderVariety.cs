using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortOrderVariety : ASortOrderVariety<
    SortItemKey<string>, 
    RedModReactiveSortItem, 
    SortItemLoadoutData<SortItemKey<string>>, 
    SortItemData<SortItemKey<string>> >
{
    private static readonly SortOrderVarietyId StaticVarietyId = SortOrderVarietyId.From(new Guid("9120C6F5-E0DD-4AD2-A99E-836F56796950"));
    
    public override SortOrderVarietyId SortOrderVarietyId => StaticVarietyId;

    public RedModSortOrderVariety(IServiceProvider serviceProvider) : base(serviceProvider) { }
    
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
        
        var ts = Connection.BeginTransaction();
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

    public override IObservable<IChangeSet<RedModReactiveSortItem, SortItemKey<string>>> GetSortOrderItemsChangeSet(SortOrderId sortOrderId)
    {
        var sortOrder = Abstractions.Loadouts.SortOrder.Load(Connection.Db, sortOrderId);
        if (!sortOrder.IsValid())
            return Observable.Empty<IChangeSet<RedModReactiveSortItem, SortItemKey<string>>>();    
        
        var parentEntity = sortOrder.ParentEntity.Match(
            loadoutId => loadoutId.Value,
            collectionGroupId => collectionGroupId.Value
        );
        var loadoutId = sortOrder.LoadoutId;
        
        // TODO: This is looking up loadout data in the entire lodaout, but if the parent entity is a collection, should we limit the lookup to just that collection?
        // TODO: Use better system to determine winner rather than modGroupId
        var result = RedModExtensions.ObserveRedModSortOrder(Connection, sortOrderId, loadoutId)
            .Transform(row =>
                {
                    var model = new RedModReactiveSortItem(
                        row.SortIndex,
                        RelativePath.FromUnsanitizedInput(row.FolderName),
                        modName: row.ModName ?? row.FolderName,
                        isActive: row.IsEnabled ?? false
                    );

                    if (row.ModGroupId == null) return model;
                    
                    model.ModGroupId = LoadoutItemGroupId.From(row.ModGroupId.Value);
                    var loadoutData = new SortItemLoadoutData<SortItemKey<string>>(
                        model.Key,
                        model.IsActive,
                        model.ModName,
                        model.ModGroupId
                    );
                    model.LoadoutData = loadoutData;
                    
                    return model;
                }
            );
        
        return result;
    }

    public override IReadOnlyList<RedModReactiveSortItem> GetSortOrderItems(SortOrderId sortOrderId, IDb? db)
    {
        var dbToUse = db ?? Connection.Db;
        var sortOrder = Abstractions.Loadouts.SortOrder.Load(dbToUse, sortOrderId);
        if (!sortOrder.IsValid())
            return [];
        
        var optionalCollection = sortOrder.ParentEntity.Match(
            loadoutId => DynamicData.Kernel.Optional<CollectionGroupId>.None,
            collectionGroupId => DynamicData.Kernel.Optional<CollectionGroupId>.Create(collectionGroupId)
        );
        
        var sortingData = RetrieveSortOrder(sortOrderId, dbToUse);
        var loadoutData = RetrieveLoadoutData(sortOrder.LoadoutId, optionalCollection,  dbToUse);
        
        // This reconcile currently removes sort items that are not in the loadout, maybe that isn't desired?
        // TODO: Consider showing items that are missing loadout data instead.
        var reconciled = Reconcile(sortingData, loadoutData);

        return reconciled.Select(tuple =>
            {
                return new RedModReactiveSortItem(
                    tuple.SortedEntry.SortIndex,
                    RelativePath.FromUnsanitizedInput(tuple.SortedEntry.Key.Key),
                    tuple.ItemLoadoutData.ModName,
                    tuple.ItemLoadoutData.IsEnabled
                )
                {
                    ModGroupId = tuple.ItemLoadoutData.ModGroupId,
                    LoadoutData = tuple.ItemLoadoutData,
                };
            }
        ).ToList();
    }

    /// <summary>
    /// Method to get the order to pass to REDMod tool.
    /// </summary>
    public IReadOnlyList<string> GetRedModOrder(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId, IDb? db = null)
    {
        var parentEntity = collectionGroupId.HasValue
            ? OneOf<LoadoutId, CollectionGroupId>.FromT1(collectionGroupId.Value)
            : OneOf<LoadoutId, CollectionGroupId>.FromT0(loadoutId);
        
        var sortOrderId = GetSortOrderIdFor(parentEntity, db);
        if (sortOrderId.HasValue)
        {
            // Return the reconciled order as list of redmod folder names, excluding disabled items
            return GetSortOrderItems(sortOrderId.Value, db)
                .Where(item => item.LoadoutData?.IsEnabled == true) 
                .Select(item => item.Key.Key)
                .ToList();
        }
        
        // There is no SortOrder for this loadout/collection in this Db revision,
        // so we just return the RedMods in the loadout, sorted by ModGroupId and FolderName
        var loadoutData = RetrieveLoadoutData(loadoutId, collectionGroupId, db);
            
        var reconciled = Reconcile([], loadoutData);

        var result = reconciled.Select(tuple =>
            {
                return new RedModReactiveSortItem(
                    tuple.SortedEntry.SortIndex,
                    RelativePath.FromUnsanitizedInput(tuple.SortedEntry.Key.Key),
                    tuple.ItemLoadoutData.ModName,
                    tuple.ItemLoadoutData.IsEnabled
                )
                {
                    ModGroupId = tuple.ItemLoadoutData.ModGroupId,
                    LoadoutData = tuple.ItemLoadoutData,
                };
            }
        ).ToList();
            
        // Return the list of redmod folder names, excluding disabled items
        return result
            .Where(item => item.LoadoutData?.IsEnabled == true)
            .Select(item => item.Key.Key)
            .ToList();
    }

    protected override void PersistSortOrderCore(
        SortOrderId sortOrderId,
        IReadOnlyList<SortItemData<SortItemKey<string>>> newOrder,
        Transaction tx,
        IDb startingDb,
        CancellationToken token = default)
    {
        var persistentSortOrderEntries = startingDb.RetrieveRedModSortableEntries(sortOrderId);
        
        token.ThrowIfCancellationRequested();
        
        // Remove outdated persistent items
        foreach (var dbItem in persistentSortOrderEntries)
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
            if (dbItem.AsSortOrderItem().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortOrderItem.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < newOrder.Count; i++)
        {
            var newItem = newOrder[i];
            if (persistentSortOrderEntries.Any(si => si.RedModFolderName == newItem.Key.Key))
                continue;

            var newDbItem = new SortOrderItem.New(tx)
            {
                ParentSortOrderId = sortOrderId,
                SortIndex = i,
            };

            _ = new RedModSortOrderItem.New(tx, newDbItem)
            {
                SortOrderItem = newDbItem,
                RedModFolderName = newItem.Key.Key,
            };
        }

        token.ThrowIfCancellationRequested();
    }
    
    /// <inheritdoc />
    protected override IReadOnlyList<SortItemData<SortItemKey<string>>> RetrieveSortOrder(SortOrderId sortOrderEntityId, IDb dbToUse)
    {
        return RedModExtensions.RetrieveRedModSortOrderItems(dbToUse, sortOrderEntityId);
    }
    
    /// <inheritdoc />
    protected override IReadOnlyList<SortItemLoadoutData<SortItemKey<string>>> RetrieveLoadoutData(LoadoutId loadoutId, DynamicData.Kernel.Optional<CollectionGroupId> collectionGroupId, IDb? db)
    {
        var dbToUse = db ?? Connection.Db;
        
        // TODO: Move query somewhere else
        // TODO: Update ranking logic to use better criteria than most recently created ModGroupId
        var result = RedModExtensions.RetrieveWinningRedModsInLoadout(dbToUse, loadoutId)
        .Select(row => new SortItemLoadoutData<SortItemKey<string>>(
            new SortItemKey<string>(row.FolderName),
            row.IsEnabled,
            row.ModName,
            row.ModGroupId == 0 ? DynamicData.Kernel.Optional<LoadoutItemGroupId>.None : LoadoutItemGroupId.From(row.Item4)
        ))
        .ToList();
        
        return result;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<(SortItemData<SortItemKey<string>> SortedEntry, SortItemLoadoutData<SortItemKey<string>> ItemLoadoutData)> Reconcile(
        IReadOnlyList<SortItemData<SortItemKey<string>>> sourceSortedEntries, 
        IReadOnlyList<SortItemLoadoutData<SortItemKey<string>>> loadoutDataItems)
    {
        var loadoutItemsDict = loadoutDataItems.ToDictionary(item => item.Key);
    
        // Start with a copy of source items
        var results = new List<(SortItemData<SortItemKey<string>> SortedEntry, SortItemLoadoutData<SortItemKey<string>> ItemLoadoutData)>(sourceSortedEntries.Count);
        var processedKeys = new HashSet<SortItemKey<string>>(sourceSortedEntries.Count);


        foreach (var sortedEntry in sourceSortedEntries)
        {
            // No matching loadout data, skip this entry
            if (!loadoutItemsDict.TryGetValue(sortedEntry.Key, out var loadoutItemData))
                continue;
            
            processedKeys.Add(sortedEntry.Key);

            // Create sort item data from sorted entry and loadout data
            results.Add((sortedEntry, loadoutItemData));
        }
    
        // Add any remaining loadout items that were not in the source sorted entries
        var itemsToAdd = loadoutItemsDict.Values
            .Where(item => !processedKeys.Contains(item.Key))
            .Order(Comparer<SortItemLoadoutData<SortItemKey<string>>>.Create((a, b) =>
            {
                // Sort by ModGroupId descending (newer items in first position), then by Key ascending
                return (a, b) switch
                {
                    (a: { ModGroupId: { HasValue: true } }, b: { ModGroupId: { HasValue: true } }) => b.ModGroupId.Value.Value.CompareTo(a.ModGroupId.Value.Value) != 0
                        ? b.ModGroupId.Value.Value.CompareTo(a.ModGroupId.Value.Value)
                        : string.Compare(a.Key.Key, b.Key.Key, StringComparison.OrdinalIgnoreCase),
                    (a: { ModGroupId: { HasValue: true } }, b: { ModGroupId: { HasValue: false } }) => 1,
                    (a: { ModGroupId: { HasValue: false } }, b: { ModGroupId: { HasValue: true } }) => -1,
                    (a: { ModGroupId: { HasValue: false } }, b: { ModGroupId: { HasValue: false } }) => string.Compare(a.Key.Key, b.Key.Key, StringComparison.OrdinalIgnoreCase),
                };

            })) 
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
