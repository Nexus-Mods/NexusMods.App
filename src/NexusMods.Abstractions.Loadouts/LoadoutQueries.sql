-- namespace: NexusMods.Abstractions.Loadouts
CREATE SCHEMA IF NOT EXISTS loadouts;

-- Returns all the entity IDs that should be tracked for a given loadout Id
CREATE MACRO loadouts.TrackedEntitiesForLoadout(db, loadoutId) AS TABLE
SELECT Id FROM mdb_LoadoutItem(Db=>db) WHERE Loadout = loadoutId
UNION ALL
SELECT Id FROM mdb_Loadout(Db=>db) WHERE Id = loadoutId
UNION ALL
SELECT Id FROM MDB_LOADOUTITEMGROUPPRIORITY(Db=>db) WHERE Loadout = loadoutId
UNION ALL
SELECT sortItem.Id FROM mdb_SortOrder(Db=>db) sortOrder
    LEFT JOIN mdb_SortOrderItem(Db=>db) sortItem ON sortOrder.Id = sortItem.ParentSortOrder
    WHERE sortOrder.Loadout = loadoutId;
