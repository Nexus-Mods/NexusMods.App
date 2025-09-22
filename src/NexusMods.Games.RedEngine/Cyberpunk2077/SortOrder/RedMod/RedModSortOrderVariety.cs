using DynamicData;
using DynamicData.Kernel;
using Microsoft.CodeAnalysis;
using NexusMods.Abstractions.GameLocators;
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

    public override IObservable<IChangeSet<RedModReactiveSortItem, SortItemKey<string>>> GetSortableItemsChangeSet(SortOrderId sortOrderId)
    {
        var sortOrder = Abstractions.Loadouts.SortOrder.Load(Connection.Db, sortOrderId);
        var parentEntity = sortOrder.ParentEntity.Match(
            loadoutId => loadoutId.Value,
            collectionGroupId => collectionGroupId.Value
        );
        var loadoutId = sortOrder.LoadoutId;
        
        // TODO: Move query somewhere else
        // TODO: deduplicate common query code
        // TODO: This is looking up loadout data in the entire lodaout, but if the parent entity is a collection, should we limit the lookup to just that collection?
        var query = Connection.Query<(string FolderName, int SortIndex, EntityId ItemId, bool? IsEnabled, string? ModName, EntityId? ModGroupId)>($"""
            WITH matchingMods AS (
                SELECT
                    regexp_extract(file.TargetPath.Item3, '^mods\/([^\/]+)\/info\.json$', 1, 'i') AS ModFolderName,
                    enabledState.IsEnabled AS IsEnabled,
                    groupItem.Name AS ModName,
                    groupItem.Id AS ModGroupId
                FROM mdb_LoadoutItemWithTargetPath(Db=>{Connection}) as file
                JOIN loadouts.LoadoutItemEnabledState({Connection}, {loadoutId}) as enabledState ON file.Id = enabledState.Id
                JOIN mdb_LoadoutItemGroup(Db=>{Connection}) as groupItem ON file.Parent = groupItem.Id
                WHERE file.TargetPath.Item1 = {loadoutId}
                    AND file.TargetPath.Item2 = {LocationId.Game.Value}
                    AND ModFolderName != ''
            ),
            loadoutData AS (
                SELECT
                    ModFolderName,
                    IsEnabled,
                    ModName,
                    ModGroupId
                FROM (
                    SELECT
                        matchingMods.*,
                        ROW_NUMBER() OVER (
                            PARTITION BY matchingMods.ModFolderName
                            ORDER BY matchingMods.IsEnabled DESC, matchingMods.ModGroupId DESC
                        ) AS ranking
                    FROM matchingMods
                ) ranked
                WHERE ranking = 1
            )
            
            SELECT sortItem.RedModFolderName, sortItem.SortIndex, sortItem.Id, loadoutData.IsEnabled, loadoutData.ModName, loadoutData.ModGroupId
            FROM mdb_RedModSortOrderItem(Db=>{Connection}) sortItem
            LEFT OUTER JOIN loadoutData on sortItem.RedModFolderName = loadoutData.ModFolderName
            WHERE sortItem.ParentSortOrder = {sortOrderId.Value}
            ORDER BY sortItem.SortIndex
            """
        );

        var result = query.Observe(table => new SortItemKey<string>(table.FolderName))
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

    public override IReadOnlyList<RedModReactiveSortItem> GetSortableItems(SortOrderId sortOrderId, IDb? db)
    {
        var dbToUse = db ?? Connection.Db;
        var sortOrder = Abstractions.Loadouts.SortOrder.Load(dbToUse, sortOrderId);
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
            if (dbItem.AsSortOrderItem().SortIndex != liveIdx)
            {
                tx.Add(dbItem, SortOrderItem.SortIndex, liveIdx);
            }
        }

        // Add new items
        for (var i = 0; i < newOrder.Count; i++)
        {
            var newItem = newOrder[i];
            if (persistentSortableEntries.Any(si => si.RedModFolderName == newItem.Key.Key))
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
        var result = Connection.Query<(string FolderName, bool IsEnabled, string ModName, EntityId ModGroupId)>($"""
                                                           WITH mod_items AS (
                                                               SELECT
                                                                   regexp_extract(file.TargetPath.Item3, '^mods\/([^\/]+)\/info\.json$', 1, 'i') AS ModFolderName,
                                                                   enabledState.IsEnabled AS IsEnabled,
                                                                   groupItem.Name AS ModName,
                                                                   groupItem.Id AS ModGroupId
                                                               FROM mdb_LoadoutItemWithTargetPath(Db=>{dbToUse}) as file
                                                               JOIN loadouts.LoadoutItemEnabledState({dbToUse}, {loadoutId}) as enabledState ON file.Id = enabledState.Id
                                                               JOIN mdb_LoadoutItemGroup(Db=>{dbToUse}) as groupItem ON file.Parent = groupItem.Id
                                                               WHERE file.TargetPath.Item1 = {loadoutId}
                                                                 AND file.TargetPath.Item2 = {LocationId.Game.Value}
                                                                 AND ModFolderName != ''
                                                           )
                                                           SELECT
                                                               ModFolderName,
                                                               IsEnabled,
                                                               ModName,
                                                               ModGroupId
                                                           FROM (
                                                               SELECT *,
                                                                      ROW_NUMBER() OVER (
                                                                          PARTITION BY ModFolderName
                                                                          ORDER BY IsEnabled DESC, ModGroupId DESC
                                                                      ) AS ranking
                                                               FROM mod_items
                                                           ) ranked
                                                           WHERE ranking = 1
                                                           """
        )
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

            // Create sortable item from sorted entry and loadout data
            results.Add((sortedEntry, loadoutItemData));
        }
    
        // Add any remaining loadout items that were not in the source sorted entries
        var itemsToAdd = loadoutItemsDict.Values
            .Where(item => !processedKeys.Contains(item.Key))
            .OrderByDescending(item => item.ModGroupId.Value.Value)
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
