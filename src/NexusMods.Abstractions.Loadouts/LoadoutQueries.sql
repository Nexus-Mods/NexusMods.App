-- namespace: NexusMods.Abstractions.Loadouts

CREATE SCHEMA IF NOT EXISTS loadouts;

-- Returns all the entity IDs that should be tracked for a given loadout Id
CREATE MACRO loadouts.TrackedEntitiesForLoadout(db, loadoutId) AS TABLE
SELECT Id FROM mdb_LoadoutItem(Db=>db) WHERE Loadout = loadoutId
UNION ALL
SELECT Id FROM mdb_Loadout(Db=>db) WHERE Id = loadoutId
UNION ALL
SELECT sortItem.Id FROM mdb_SortOrder(Db=>db) sortOrder
    LEFT JOIN mdb_SortOrderItem(Db=>db) sortItem ON sortOrder.Id = sortItem.ParentSortOrder
    WHERE sortOrder.Loadout = loadoutId;
       
-- Returns the enabled state of the collections for a given loadout
CREATE MACRO loadouts.CollectionEnabledState(db, loadoutId) AS TABLE
SELECT
    coll_table.Id AS Id,
    coll_table.Disabled = FALSE AS IsEnabled
FROM mdb_CollectionGroup(Db=>db) coll_table
WHERE coll_table.Loadout = loadoutId;

-- Returns the enabled state of the item groups for a given loadout
-- does not include the collection groups
CREATE MACRO loadouts.ItemGroupEnabledState(db, loadoutId) AS TABLE
SELECT
    group_table.Id AS Id,
    group_table.Disabled = FALSE AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
FROM mdb_LoadoutItemGroup(Db=>db) group_table
         LEFT JOIN mdb_CollectionGroup(Db=>db) coll_table ON group_table.Parent = coll_table.Id
    -- Exclude groups that are collections
         LEFT JOIN mdb_CollectionGroup(Db=>db) isColl_table ON group_table.Id = isColl_table.Id
WHERE isColl_table.Id IS NULL
AND group_table.Loadout = loadoutId;

-- Returns the enabled state of the items for a given loadout
-- does not include the collection groups or item groups
CREATE MACRO loadouts.LoadoutItemEnabledState(db, loadoutId) AS TABLE
SELECT
    item_table.Id AS Id,
    item_table.Disabled = FALSE 
        AND group_table.Disabled = FALSE
        AND COALESCE(coll_table.Disabled, FALSE) = FALSE AS IsEnabled
FROM mdb_LoadoutItem(Db=>db) item_table
JOIN mdb_LoadoutItemGroup(Db=>db) group_table ON item_table.Parent = group_table.Id
LEFT JOIN mdb_CollectionGroup(Db=>db) coll_table ON group_table.Parent = coll_table.Id
-- Exclude items that are groups themselves
LEFT JOIN mdb_LoadoutItemGroup(Db=>db) isGroup_table ON item_table.Id = isGroup_table.Id
WHERE isGroup_table.Id IS NULL
AND item_table.Loadout = loadoutId;


--- Finds all the loadout items that have a target path that are enabled
CREATE MACRO loadouts.EnabledLoadoutItemWithTargetPathInLoadout(db, loadoutId) AS TABLE
SELECT item_table.Id, item_table.TargetPath
FROM mdb_LoadoutItemWithTargetPath(Db=>db) item_table
JOIN mdb_LoadoutItemGroup(Db=>db) group_table 
    ON item_table.Parent = group_table.Id
    AND group_table.Loadout = loadoutId
    AND group_table.Disabled = FALSE
LEFT JOIN mdb_CollectionGroup(Db=>db) coll_table 
    ON group_table.Parent = coll_table.Id
    AND coll_table.Loadout = loadoutId
WHERE item_table.Loadout = loadoutId
    AND (coll_table.Disabled IS NULL OR coll_table.Disabled = FALSE);

-- Returns all the enabled targeted files, returning their id, path, hash, size and a flag
-- if the file is deleted or not
CREATE MACRO loadouts.EnabledFilesWithMetadata(db, loadoutId) AS TABLE
SELECT items.Id, items.TargetPath, file.Hash, file.Size, deleted.Id is NOT NULL as IsDeleted 
FROM loadouts.EnabledLoadoutItemWithTargetPathInLoadout(db, loadoutId) items
LEFT JOIN mdb_LoadoutFile(Db=>db) file ON file.Id = items.Id
LEFT JOIN mdb_DeletedFile(Db=>db) deleted ON deleted.Id = items.Id;

CREATE MACRO loadouts.FileConflicts(db, loadoutId) AS TABLE
SELECT
    TargetPath.Item2,
    TargetPath.Item3,
    LIST((Id, IsDeleted))
FROM loadouts.EnabledFilesWithMetadata(db, loadoutId)
GROUP BY TargetPath.Item2, TargetPath.Item3
HAVING len(LIST((Id, Hash))) >= 2;
