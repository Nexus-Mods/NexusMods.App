using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts.Rows;
using NexusMods.Cascade;
using NexusMods.Cascade.Flows;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.Loadouts;


public partial class Loadout
{
    /// <summary>
    ///  Returns all items in a loadout
    /// </summary>
    public static readonly Flow<(EntityId Loadout, EntityId Entity)> LoadoutItemsFlow =
        Pattern.Create()
            .Db(out var loadoutItem, LoadoutItem.LoadoutId, out var loadoutId)
            .Return(loadoutId, loadoutItem);

    /// <summary>
    /// Include the loadout itself
    /// </summary>
    private static readonly Flow<(EntityId Loadout, EntityId LoadoutEntity)> LoadoutEntityFlow =
        Pattern.Create()
            .Db(out var loadoutEntity, Loadout.Name, out _)
            .Return(loadoutEntity, loadoutEntity);

    /// <summary>
    /// A union of all the entities associated with a loadout. The result of this query is a tuple of the loadout id and the entity id of the
    /// most recent transaction for that entity. If more entities need to be tracked, add another .With() call to this flow.
    /// </summary>
    public static readonly UnionFlow<(EntityId Loadout, EntityId Entity)> LoadoutAssociatedEntitiesFlow =
        new UnionFlow<(EntityId Loadout, EntityId Entity)>(LoadoutEntityFlow)
            .With(LoadoutItemsFlow);


    /// <summary>
    /// Calculates the most recent transaction for a loadout. Pretty simple, we just group by the loadout id and take the max transaction id.
    /// But this means that the row updates whenever any of the tracked entities are updated, meaning we can simply watch this row or the specific
    /// cell on the row to know whenever something modifies the loadout.
    /// </summary>
    public static readonly Flow<MostRecentTxForLoadoutRow> MostRecentTxForLoadoutFlow =
        Pattern.Create()
            .Match(LoadoutAssociatedEntitiesFlow, out var loadout, out var entity)
            .DbLatestTx(entity, out var maxTx)
            // Track the count as well, so that we know when items are removed. Removing an item
            // may not change the max TxId, but removal will change the count of items being tracked
            .ReturnMostRecentTxForLoadoutRow(loadout, maxTx.Max(), entity.Count());

    /// <summary>
    /// Returns all mutable collection groups in a loadout.
    /// </summary>
    public static readonly Flow<(EntityId CollectionGroup, EntityId Loadout)> MutableCollectionsFlow = Pattern.Create()
        .Db(out var collectionEntityId, CollectionGroup.IsReadOnly, out var isReadOnly)
        .Db(collectionEntityId, LoadoutItem.LoadoutId, out var loadoutEntityId)
        .Return(collectionEntityId, loadoutEntityId, isReadOnly)
        .Where(tuple => !tuple.Item3)
        .Select(tuple => (tuple.Item1, tuple.Item2));

    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        // A bit wordy, to have to specify all the generic types here, eventually we'll add a simpler method via Cascade to do this. 
        return connection.Topology
            .Observe(MostRecentTxForLoadoutFlow.Where(row => row.LoadoutId == id.Value))
            .Select(_ => Loadout.Load(connection.Db, id));
    }
    
    /// <summary>
    /// Returns collection ids and their enabled state
    /// </summary>
    public static readonly Flow<(EntityId CollectionId, bool IsCollectionEnabled)> IsCollectionEnabledFlow =
        Pattern.Create().Db(out var collectionId, CollectionGroup.IsReadOnly, out _)
            .DbOrDefault(Query.Db, collectionId, LoadoutItem.Disabled, out var collectionDisabled, default(Null))
            // Collection is enabled if the Disabled attribute is missing, so if it is default.
            .Project(collectionDisabled, disabled => disabled.IsDefault, out var isCollectionEnabled)
            .Return(collectionId, isCollectionEnabled);
    
    /// <summary>
    /// Returns whether a collection is enabled or not
    /// </summary>
    public static async ValueTask<bool> IsCollectionEnabled(IDb db, EntityId collectionId)
    {
        using var isEnabled = await db.Topology.QueryAsync(
            IsCollectionEnabledFlow.Where(row => row.CollectionId == collectionId));
        return isEnabled.Count == 1 && isEnabled.First().IsCollectionEnabled;
    }
    
    /// <summary>
    /// Returns loadoutItemGroup ids and their enabled state, also considering the parent collection's enabled state.
    /// </summary>
    public static readonly Flow<(EntityId GroupId, bool IsModEnabled)> IsLoadoutItemGroupEnabledFlow =
        Pattern.Create()
            .Db(out var groupId, LoadoutItemGroup.Group, out _)
            .DbOrDefault(Query.Db, groupId, LoadoutItem.Disabled, out var modDisabled, default(Null))
            // Group is enabled if the Disabled attribute is missing, so if it is default.
            .Project(modDisabled, disabled => disabled.IsDefault, out var isModEnabled)
            // .Return(groupId, isModEnabled);
            // Some groups my not have a parent collection, so in that case we take true as collectionIsEnabled value.
            .DbOrDefault(groupId, LoadoutItem.Parent, out var collectionId, default(EntityId))
            .MatchDefault(IsCollectionEnabledFlow.Rekey(row => row.CollectionId), 
                collectionId, out var collectionIsEnabledData, 
                default(EntityId), (CollectionId: default(EntityId), IsCollectionEnabled: true))
            .Project(collectionIsEnabledData, row => row.IsCollectionEnabled, out var collectionIsEnabled)
            .Return(groupId, collectionIsEnabled ,isModEnabled)
            .Select(row => (row.Item1, row.Item2 && row.Item3)); 
    
    /// <summary>
    /// Returns whether a loadoutItemGroup is enabled or not, also considering the parent collection's enabled state.
    /// </summary>
    public static async ValueTask<bool> IsLoadoutItemGroupEnabled(IDb db, EntityId loadoutItemGroupId)
    {
        using var isEnabled = await db.Topology.QueryAsync(
            IsLoadoutItemGroupEnabledFlow.Where(row => row.GroupId == loadoutItemGroupId)
        );
        return isEnabled.Count == 1 && isEnabled.First().IsModEnabled;
    }
    
    /// <summary>
    /// Returns loadoutItem ids and their enabled state, also considering the enabled states of the parent group and collection.
    /// <remarks>This only works for children of LoaodutGroups, such as mod files, and not for direct children to a loadout such as the groups</remarks>
    /// </summary>
    public static readonly Flow<(EntityId LoaodutItemId, bool IsLoadoutItemEnabled)> IsLoadoutItemEnabledFlow =
        Pattern.Create().Db(out var itemId, LoadoutItem.Parent, out var groupId)
            .Match(IsLoadoutItemGroupEnabledFlow, groupId, out var parentGroupIsEnabled)
            .DbOrDefault(Query.Db, itemId, LoadoutItem.Disabled, out var itemDisabled, default(Null))
            // Item is enabled if the Disabled attribute is missing, so if it is default.
            .Project(itemDisabled, disabled => disabled.IsDefault, out var isItemEnabled)
            .Return(itemId, parentGroupIsEnabled ,isItemEnabled)
            .Select(row => (row.Item1, row.Item2 && row.Item3)); 
    
    /// <summary>
    /// Returns whether a loadoutItem is enabled or not, also considering the enabled states of the parent group and collection.
    /// </summary>
    public static async ValueTask<bool> IsLoadoutItemEnabled(IDb db, EntityId loadoutItemId)
    {
        using var isEnabled = await db.Topology.QueryAsync(
            IsLoadoutItemEnabledFlow.Where(row => row.LoaodutItemId == loadoutItemId)
        );
        return isEnabled.Count == 1 && isEnabled.First().IsLoadoutItemEnabled;
    }

    /// <summary>
    /// Returns all enabled LoadoutItemsWithTargetPath in a loadout.
    /// </summary>
    public static readonly Flow<(EntityId LoadoutId, EntityId LoadoutItemWithTargetPathId)> EnabledLoadoutItemsWithTargetPathFlow =
        Pattern.Create()
            .Db(out var itemId, LoadoutItemWithTargetPath.TargetPath, out _)
            .Db(itemId, LoadoutItem.LoadoutId, out var loadoutId)
            .Match(IsLoadoutItemEnabledFlow.Where(row => row.IsLoadoutItemEnabled), itemId, out _)
            .Return(loadoutId, itemId);
            // .Select(row => (LoadoutId: row.Item1, LoadoutItemWithTargetPathId: row.Item2));

    /// <summary>
    /// Returns all enabled LoadoutItemsWithTargetPath in a loadout.
    /// </summary>
    public static IEnumerable<LoadoutItemWithTargetPath.ReadOnly> GetEnabledLoadoutItemsWithTargetPath(IDb db, LoadoutId loadoutId)
    {
        using var items = db.Topology.Query(
            EnabledLoadoutItemsWithTargetPathFlow.Where(row => row.LoadoutId == loadoutId.Value)
        );
        return items.Select(row => LoadoutItemWithTargetPath.Load(db, row.LoadoutItemWithTargetPathId)).ToArray();
    }
    
    /// <summary>
    /// Returns all enabled LoadoutItemsWithTargetPath in a loadout.
    /// </summary>
    public static async Task<IEnumerable<LoadoutItemWithTargetPath.ReadOnly>> GetEnabledLoadoutItemsWithTargetPathAsync(IDb db, LoadoutId loadoutId)
    {
        using var items = await db.Topology.QueryAsync(
            EnabledLoadoutItemsWithTargetPathFlow.Where(row => row.LoadoutId == loadoutId.Value)
        );
        return items.Select(row => LoadoutItemWithTargetPath.Load(db, row.LoadoutItemWithTargetPathId)).ToArray();
    }
}
