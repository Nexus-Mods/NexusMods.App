using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Mutagen.Bethesda.Plugins;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using OneOf;

namespace NexusMods.Games.CreationEngine.LoadOrder;

public class PluginLoadOrderVariety : ASortOrderVariety<
    SortItemKey<ModKey>,
    PluginReactiveSortItem,
    SortItemLoadoutData<SortItemKey<ModKey>>,
    SortItemData<SortItemKey<ModKey>> >
{
    private static readonly SortOrderVarietyId StaticVarietyId = SortOrderVarietyId.From(new Guid("949FC50A-C4BA-49EB-9A82-B3F2BE5849E2"));
    
    public PluginLoadOrderVariety(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override SortOrderUiMetadata SortOrderUiMetadata { get; } = new()
    {
        SortOrderName = "Plugins",
        OverrideInfoTitle = "The load order of game plugin files, last one to load wins.",
        OverrideInfoMessage = "",
        DisplayNameColumnHeader = "Plugin File Name",
        EmptyStateMessageTitle = "No Plugins detected",
        EmptyStateMessageContents = "No plugins were detected.",
        WinnerIndexToolTip = "The last loaded plugin will overwrite data from plugins loaded before it.",
        IndexColumnHeader = "Load Order",
        LearnMoreUrl = "",
    };

    public override SortOrderVarietyId SortOrderVarietyId => StaticVarietyId;
    public override async ValueTask<SortOrderId> GetOrCreateSortOrderFor(LoadoutId loadoutId, OneOf<LoadoutId, CollectionGroupId> parentEntity, CancellationToken token = default)
    {
        var optionalSortOrderId = GetSortOrderIdFor(parentEntity);
        if (optionalSortOrderId.HasValue)
            return optionalSortOrderId.Value;
        
        token.ThrowIfCancellationRequested();
        
        using var ts = Connection.BeginTransaction();
        var newSortOrder = new SortOrder.New(ts)
        {
            LoadoutId = loadoutId,
            ParentEntity = parentEntity,
            SortOrderTypeId = SortOrderVarietyId.Value,
        };
        
        var commitResult = await ts.Commit();

        var sortOrder = commitResult.Remap(newSortOrder);
        return sortOrder;
    }

    public override IObservable<IChangeSet<PluginReactiveSortItem, SortItemKey<ModKey>>> GetSortOrderItemsChangeSet(SortOrderId sortOrderId)
    {
        var result = Connection.Query<(ModKey Key, string GroupName, EntityId? ModId, int SortIndex)>(
                $"""
                 SELECT ModKey, GroupName, GroupId, SortIndex
                 FROM creation_engine.plugin_sort_order({Connection}) WHERE SortOrderId = {sortOrderId.Value}
                 """)
            .Observe(x => new SortItemKey<ModKey>(x.Key))
            .Transform(x =>
                {
                    var model = new PluginReactiveSortItem(x.SortIndex, x.Key, x.GroupName, false);

                    if (!x.ModId.HasValue)
                        return model;

                    model.ModGroupId = Optional<LoadoutItemGroupId>.Create(x.ModId!.Value);
                    var loadoutData = new SortItemLoadoutData<SortItemKey<ModKey>>(
                        model.Key,
                        true, 
                        x.GroupName, 
                        LoadoutItemGroupId.From(x.ModId!.Value));
                    
                    model.LoadoutData = loadoutData;
                    return model;
                }
            );
        return result;
    }

    public override IReadOnlyList<PluginReactiveSortItem> GetSortOrderItems(SortOrderId sortOrderId, IDb? db)
    {
        var dbToUse = db ?? Connection.Db;
        var sortOrder = SortOrder.Load(dbToUse, sortOrderId);
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
                return new PluginReactiveSortItem(
                    tuple.SortedEntry.SortIndex,
                    tuple.SortedEntry.Key.Key,
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

    protected override IReadOnlyList<SortItemData<SortItemKey<ModKey>>> RetrieveSortOrder(SortOrderId sortOrderId, IDb db)
    {
        return RetrievePluginSortOrder(sortOrderId, db)
            .Select(row => new SortItemData<SortItemKey<ModKey>>(
                new SortItemKey<ModKey>(row.ModKey),
                row.AsSortOrderItem().SortIndex
            ))
            .OrderBy(r => r.SortIndex)
            .ToList();
    }

    protected override void PersistSortOrderCore(SortOrderId sortOrderId, IReadOnlyList<SortItemData<SortItemKey<ModKey>>> newOrder, ITransaction tx, IDb startingDb, CancellationToken token = default)
    {
        var persistedEntries = RetrievePluginSortOrder(sortOrderId, startingDb);
        
        token.ThrowIfCancellationRequested();
        
        foreach (var dbItem in persistedEntries)
        {
            var newItem = newOrder.FirstOrOptional(newItem => newItem.Key.Key.Equals(dbItem.ModKey));

            if (!newItem.HasValue)
            {
                tx.Delete(dbItem.Id, recursive: false);
                continue;
            }

            var liveIdx = newOrder.IndexOf(newItem.Value);

            if (dbItem.AsSortOrderItem().SortIndex != liveIdx)
            {
                tx.Add(dbItem.Id, SortOrderItem.SortIndex, liveIdx);
            }
        }
        
        // Add new items
        for (var i = 0; i < newOrder.Count; i++)
        {
            var newItem = newOrder[i];
            if (persistedEntries.Any(si => si.ModKey == newItem.Key.Key))
                continue;

            var newDbItem = new SortOrderItem.New(tx)
            {
                ParentSortOrderId = sortOrderId,
                SortIndex = i,
            };

            _ = new PluginSortEntry.New(tx, newDbItem)
            {
                SortOrderItem = newDbItem,
                ModKey = newItem.Key.Key,
            };
        }

        token.ThrowIfCancellationRequested();
    }

    protected PluginSortEntry.ReadOnly[] RetrievePluginSortOrder(SortOrderId sortOrderId, IDb db)
    {
        return SortOrderItem.FindByParentSortOrder(db, sortOrderId).OfTypePluginSortEntry().ToArray();
    }

    protected override IReadOnlyList<SortItemLoadoutData<SortItemKey<ModKey>>> RetrieveLoadoutData(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId, IDb? db)
    {
        var dbToUse = db ?? Connection.Db;

        var result = dbToUse.Connection.Query<(ModKey ModKey, EntityId? GroupId, string? GroupName)>(
                $"SELECT ModKey, GroupId, GroupName FROM creation_engine.load_order_plugin_files({dbToUse}) WHERE Loadout = {loadoutId}")
            .Select(row => new SortItemLoadoutData<SortItemKey<ModKey>>(
                new SortItemKey<ModKey>(row.ModKey),
                true,
                row.GroupName ?? "UNKNOWN",
                row.GroupId != null ? Optional<LoadoutItemGroupId>.Create(row.GroupId!.Value) : Optional<LoadoutItemGroupId>.None
                )).ToList();
        
        return result;
    }

    protected override IReadOnlyList<(SortItemData<SortItemKey<ModKey>> SortedEntry, SortItemLoadoutData<SortItemKey<ModKey>> ItemLoadoutData)> Reconcile(IReadOnlyList<SortItemData<SortItemKey<ModKey>>> sourceSortedEntries, IReadOnlyList<SortItemLoadoutData<SortItemKey<ModKey>>> loadoutDataItems)
    {
        var loadoutItemsDict = loadoutDataItems.ToDictionary(item => item.Key);
    
        // Start with a copy of source items
        var results = new List<(SortItemData<SortItemKey<ModKey>> SortedEntry, SortItemLoadoutData<SortItemKey<ModKey>> ItemLoadoutData)>(sourceSortedEntries.Count);
        var processedKeys = new HashSet<SortItemKey<ModKey>>(sourceSortedEntries.Count);


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
            .Order(Comparer<SortItemLoadoutData<SortItemKey<ModKey>>>.Create((a, b) =>
            {
                // Sort by ModGroupId ascending (newer items in last position), then by Key ascending
                return (a, b) switch
                {
                    (a: { ModGroupId: { HasValue: true } }, b: { ModGroupId: { HasValue: true } }) => a.ModGroupId.Value.Value.CompareTo(b.ModGroupId.Value.Value) != 0
                        ? a.ModGroupId.Value.Value.CompareTo(b.ModGroupId.Value.Value)
                        : string.Compare(a.Key.Key.ToString(), b.Key.Key.ToString(), StringComparison.OrdinalIgnoreCase),
                    (a: { ModGroupId: { HasValue: true } }, b: { ModGroupId: { HasValue: false } }) => 1,
                    (a: { ModGroupId: { HasValue: false } }, b: { ModGroupId: { HasValue: true } }) => -1,
                    (a: { ModGroupId: { HasValue: false } }, b: { ModGroupId: { HasValue: false } }) => string.Compare(a.Key.Key.ToString(), b.Key.Key.ToString(), StringComparison.OrdinalIgnoreCase),
                };

            })) 
            .Select(loadoutItemData => (
                new SortItemData<SortItemKey<ModKey>>(loadoutItemData.Key, 0), // SortIndex will be updated later
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
