using System.Data.Common;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts.Rows;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts;


public partial class Loadout
{

    private const string TrackedEntitiesForLoadout =
        """
        SELECT Id FROM mdb_LoadoutItem(Db=>$1) WHERE Loadout = $2
        UNION ALL
        SELECT Id FROM mdb_Loadout(Db=>$1) WHERE Id = $2
        UNION ALL
        SELECT sortItem.Id FROM mdb_SortOrder(Db=>$1) sortOrder
           LEFT JOIN mdb_SortOrderItem(Db=>$1) sortItem ON sortOrder.Id = sortItem.ParentSortOrder
           WHERE sortOrder.Loadout = $2
        """;

    private const string Revisions =
        $"""
        SELECT $2, MAX(d.T), COUNT(d.E) FROM 
        ({TrackedEntitiesForLoadout}) ents
        LEFT JOIN mdb_Datoms() d ON d.E = ents.Id
        """;

    private const string IsCollectionEnabledSql =
        """
        SELECT collection_table.Id, collection_table.Disabled = FALSE AS IsEnabled 
        FROM mdb_CollectionGroup(Db=>$1) collection_table
        WHERE collection_table.Id = $2
        """;
    
    private const string IsLoadoutItemGroupEnabledSql =
        """
        SELECT 
            group_table.Id, 
            (group_table.Disabled = FALSE 
                 AND (collection_table.Disabled IS NULL OR collection_table.Disabled = FALSE))AS IsEnabled 
        FROM mdb_LoadoutItemGroup(Db=>$1) group_table
        LEFT JOIN mdb_CollectionGroup(Db=>$1) collection_table ON Parent = collection_table.Id
        WHERE group_table.Id = $2
        """;
    
    private const string IsLoadoutItemEnabledSql =
        """
        SELECT 
            item_table.Id, 
            (item_table.Disabled = FALSE 
                AND (group_table.Disabled IS NULL OR group_table.Disabled = FALSE)
                AND (collection_table.Disabled IS NULL OR collection_table.Disabled = FALSE)) AS IsEnabled
        FROM mdb_LoadoutItem(Db=>$1) item_table
        LEFT JOIN mdb_LoadoutItemGroup(Db=>$1) group_table ON item_table.Parent = group_table.Id
        LEFT JOIN mdb_CollectionGroup(Db=>$1) collection_table ON group_table.Parent = collection_table.Id
        WHERE item_table.Id = $2
        """;
    
    private const string CollectionsEnabledStateSql =
        """
        SELECT Id, Disabled = FALSE AS IsEnabled 
        FROM mdb_CollectionGroup(Db=>$1) 
        WHERE Loadout = $2
        """;
    
    private const string LoadoutItemGroupsEnabledStateSql =
        """
        SELECT 
            group_table.Id AS GroupId, 
            (group_table.Disabled = FALSE 
                AND (parent_table.Disabled IS NULL OR parent_table.Disabled = FALSE)) AS IsEnabled
        FROM mdb_LoadoutItemGroup(Db=>$1) group_table
        LEFT JOIN mdb_CollectionGroup(Db=>$1) parent_table ON group_table.Parent = parent_table.Id
        WHERE group_table.Loadout = $2
        """;
    
    private const string LoadoutItemsEnabledStateSql =
        """
        SELECT 
            item_table.Id AS ItemId, 
            (item_table.Disabled = FALSE 
                AND (group_table.Disabled IS NULL OR group_table.Disabled = FALSE)
                AND (collection_table.Disabled IS NULL OR collection_table.Disabled = FALSE)) AS IsEnabled
        FROM mdb_LoadoutItem(Db=>$1) item_table
        LEFT JOIN mdb_LoadoutItemGroup(Db=>$1) group_table ON item_table.Parent = group_table.Id
        LEFT JOIN mdb_CollectionGroup(Db=>$1) collection_table ON group_table.Parent = collection_table.Id
        WHERE item_table.Loadout = $2
        """;

    /// <summary>
    /// Returns all mutable collection groups in a loadout.
    /// </summary>
    public static Query<(EntityId GroupId, string Name)> MutableCollections(IConnection connection, LoadoutId id) =>
        connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_CollectionGroup(Db=>$1) WHERE IsReadOnly = false AND Loadout = $2 ORDER BY Id", connection, id.Value);


    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        return connection.Query<(EntityId LoadoutID, ulong Max, long Count)>(Revisions, connection, id.Value)
            .Observe(x => x.LoadoutID)
            .QueryWhenChanged(_ => Loadout.Load(connection.Db, id));
    }

    public static Optional<bool> IsCollectionEnabled(IConnection connection, CollectionGroupId groupId)
    {
        return connection.Query<(EntityId GroupId, bool IsEnabled)>(IsCollectionEnabledSql, connection, groupId.Value)
            .Select(x => x.IsEnabled)
            .FirstOrOptional(_ => true);
    }
    
    public static Optional<bool> IsLoadoutItemGroupEnabled(IConnection connection, LoadoutItemGroupId groupId)
    {
        return connection.Query<(EntityId GroupId, bool IsEnabled)>(IsLoadoutItemGroupEnabledSql, connection, groupId.Value)
            .Select(x => x.IsEnabled)
            .FirstOrOptional(_ => true);
    }
    
    public static Optional<bool> IsLoadoutItemEnabled(IConnection connection, LoadoutItemId itemId)
    {
        return connection.Query<(EntityId ItemId, bool IsEnabled)>(IsLoadoutItemEnabledSql, connection, itemId.Value)
            .Select(x => x.IsEnabled)
            .FirstOrOptional(_ => true);
    }

    public static Query<(EntityId CollectionId, bool IsEnabled)> CollectionsEnabledState(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId CollectionId, bool IsEnabled)>(CollectionsEnabledStateSql, connection, loadoutId.Value);
    }
    
    public static Query<(EntityId GroupId, bool IsEnabled)> LoadoutItemGroupsEnabledState(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId GroupId, bool IsEnabled)>(LoadoutItemGroupsEnabledStateSql, connection, loadoutId.Value);
    }
    
    public static Query<(EntityId ItemId, bool IsEnabled)> LoadoutItemsEnabledState(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId ItemId, bool IsEnabled)>(LoadoutItemsEnabledStateSql, connection, loadoutId.Value);
    }
}
