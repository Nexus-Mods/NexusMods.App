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
    
    
#region Enabled State Queries
    
    
    private const string CollectionEnabledStateSql =
        """
        SELECT 
            coll_table.Id AS Id, 
            coll_table.Disabled = FALSE AS IsEnabled 
        FROM mdb_CollectionGroup(Db=>$1) coll_table
        """;
    
    private const string CollectionEnabledStateInLoadoutSql =
        """
        SELECT 
            coll_table.Id AS Id, 
            coll_table.Disabled = FALSE AS IsEnabled 
        FROM mdb_CollectionGroup(Db=>$1) coll_table
        
        WHERE coll_table.Loadout = $2
        """;
    
    private const string IsCollectionEnabledSql =
        """
        SELECT 
            coll_table.Id AS Id, 
            coll_table.Disabled = FALSE AS IsEnabled 
        FROM mdb_CollectionGroup(Db=>$1) coll_table
        
        WHERE coll_table.Id = $2
        """;
    
    
    /// <summary>
    /// This excludes groups that are collections
    /// </summary>
    private const string LoadoutItemGroupEnabledStateSql =
        """
        SELECT 
            group_table.Id AS Id, 
            group_table.Disabled = FALSE AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItemGroup(Db=>$1) group_table
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        -- Exclude groups that are collections
        LEFT JOIN mdb_CollectionGroup(Db=>$1) isColl_table ON group_table.Id = isColl_table.Id
        WHERE isColl_table.Id IS NULL
        """;
    
    private const string LoadoutItemGroupEnabledStateInLoadoutSql =
        """
        SELECT 
            group_table.Id AS Id, 
            group_table.Disabled = FALSE AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItemGroup(Db=>$1) group_table
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        -- Exclude groups that are collections
        LEFT JOIN mdb_CollectionGroup(Db=>$1) isColl_table ON group_table.Id = isColl_table.Id
        WHERE isColl_table.Id IS NULL
        
        AND group_table.Loadout = $2
        """;
    
    private const string IsLoadoutItemGroupEnabledSql =
        """
        SELECT 
            group_table.Id AS Id, 
            group_table.Disabled = FALSE AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItemGroup(Db=>$1) group_table
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        
        WHERE group_table.Id = $2
        """;
    
    
    /// <summary>
    /// This excludes items that are groups themselves.
    /// </summary>
    private const string LoadoutItemsEnabledStateSql =
        """
        SELECT 
            item_table.Id AS ItemId, 
            item_table.Disabled = FALSE 
                AND COALESCE(group_table.Disabled, FALSE) = FALSE
                AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItem(Db=>$1) item_table
        JOIN mdb_LoadoutItemGroup(Db=>$1) group_table ON item_table.Parent = group_table.Id
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        -- Exclude items that are groups themselves
        LEFT JOIN mdb_LoadoutItemGroup(Db=>$1) isGroup_table ON item_table.Id = isGroup_table.Id
        WHERE isGroup_table.Id IS NULL
        """;

    private const string LoadoutItemsEnabledStateInLoadoutSql =
        """
        SELECT 
            item_table.Id AS ItemId, 
            item_table.Disabled = FALSE 
                AND COALESCE(group_table.Disabled, FALSE) = FALSE
                AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItem(Db=>$1) item_table
        JOIN mdb_LoadoutItemGroup(Db=>$1) group_table ON item_table.Parent = group_table.Id
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        -- Exclude items that are groups themselves
        LEFT JOIN mdb_LoadoutItemGroup(Db=>$1) isGroup_table ON item_table.Id = isGroup_table.Id
        WHERE isGroup_table.Id IS NULL 
            
        AND item_table.Loadout = $2
        """;
    
    private const string IsLoadoutItemEnabledSql =
        """
        SELECT 
            item_table.Id AS ItemId, 
            item_table.Disabled = FALSE 
                AND COALESCE(group_table.Disabled, FALSE) = FALSE
                AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
        FROM mdb_LoadoutItem(Db=>$1) item_table
        JOIN mdb_LoadoutItemGroup(Db=>$1) group_table ON item_table.Parent = group_table.Id
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table ON group_table.Parent = coll_table.Id
        
        WHERE item_table.Id = $2
        """;
    
#endregion Enabled State Queries
    
    private const string EnabledLoadoutItemWithTargetPathInLoadoutSql =
        """
        SELECT item_table.Id
        FROM mdb_LoadoutItemWithTargetPath(Db=>$1) item_table
        JOIN mdb_LoadoutItemGroup(Db=>$1) group_table 
            ON item_table.Parent = group_table.Id
            AND group_table.Loadout = $2
            AND group_table.Disabled = FALSE
        LEFT JOIN mdb_CollectionGroup(Db=>$1) coll_table 
            ON group_table.Parent = coll_table.Id
            AND coll_table.Loadout = $2
        WHERE item_table.Loadout = $2
            AND (coll_table.Disabled IS NULL OR coll_table.Disabled = FALSE)
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

    public static Query<(EntityId CollectionId, bool IsEnabled)> CollectionEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId CollectionId, bool IsEnabled)>(CollectionEnabledStateInLoadoutSql, connection, loadoutId.Value);
    }
    
    public static Query<(EntityId GroupId, bool IsEnabled)> LoadoutItemGroupEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId GroupId, bool IsEnabled)>(LoadoutItemGroupEnabledStateInLoadoutSql, connection, loadoutId.Value);
    }
    
    public static Query<(EntityId ItemId, bool IsEnabled)> LoadoutItemEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId ItemId, bool IsEnabled)>(LoadoutItemsEnabledStateInLoadoutSql, connection, loadoutId.Value);
    }
    
    public static IEnumerable<LoadoutItemWithTargetPath.ReadOnly> EnabledLoadoutItemWithTargetPathInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return LoadoutItemWithTargetPath.Load(
            connection.Db,
            connection.Query<EntityId>(EnabledLoadoutItemWithTargetPathInLoadoutSql, connection, loadoutId.Value)
        );
    }
}
