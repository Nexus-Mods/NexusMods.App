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

-- Returns every leaf loadout item id and enabled state
CREATE OR REPLACE MACRO loadouts.LoadoutItemIsEnabled (db, loadoutId) AS TABLE
SELECT
  loadout_item.Id,
  loadout_item.Disabled = FALSE
  AND loadout_group.Disabled = FALSE
  AND COALESCE(installed_collection.Disabled, FALSE) = FALSE AS IsEnabled
FROM
  MDB_LOADOUTITEM (Db => db) loadout_item
  JOIN MDB_LOADOUTITEMGROUP (Db => db) loadout_group ON loadout_item.Parent = loadout_group.Id
  LEFT JOIN MDB_COLLECTIONGROUP (Db => db) installed_collection ON loadout_group.Parent = installed_collection.Id
  LEFT JOIN MDB_LOADOUTITEMGROUP (Db => db) item_as_group ON loadout_item.Id = item_as_group.Id
WHERE
  item_as_group.Id IS NULL
  AND loadout_item.Loadout = loadoutId;

-- Returns every loadout item with target path and enabled state
CREATE OR REPLACE MACRO loadouts.LoadoutPathItemIsEnabled (db, loadoutId, onlyEnabled) AS TABLE
SELECT
  item.Id,
  item.IsEnabled,
  item_with_path.TargetPath,
FROM
  loadouts.LoadoutItemIsEnabled (db, loadoutId) item
  JOIN MDB_LOADOUTITEMWITHTARGETPATH (Db => db) item_with_path ON item.Id = item_with_path.Id
WHERE
  onlyEnabled = FALSE
  OR item.IsEnabled = onlyEnabled;

-- Returns every loadout file with target path, hash, size, enabled and deleted state
CREATE OR REPLACE MACRO loadouts.LoadoutFileMetadata (db, loadoutId, onlyEnabled) AS TABLE
SELECT
  item.Id,
  item.IsEnabled,
  item.TargetPath,
  loadout_file.Hash,
  loadout_file.Size,
  deleted_file.Id IS NOT NULL as IsDeleted
FROM
  loadouts.LoadoutPathItemIsEnabled (db, loadoutId, onlyEnabled) item
  LEFT JOIN MDB_LOADOUTFILE (Db => db) loadout_file ON item.Id = loadout_file.Id
  LEFT JOIN MDB_DELETEDFILE (Db => db) deleted_file ON item.Id = deleted_file.Id;

-- Returns all file conflict groups
CREATE OR REPLACE MACRO loadouts.FileConflicts (db, loadoutId, removeDuplicates) AS TABLE
SELECT
  TargetPath.Item2,
  TargetPath.Item3,
  LIST(STRUCT_PACK(Id := Id, IsEnabled := IsEnabled, IsDeleted := IsDeleted)) AS Conflicts
FROM
  loadouts.LoadoutFileMetadata (db, loadoutId, FALSE)
GROUP BY
  TargetPath.Item2,
  TargetPath.Item3
HAVING
  len(LIST(Id)) >= 2
  AND (
    NOT removeDuplicates
    OR COUNT(DISTINCT Hash) > 1
  );

-- Returns all file conflicts grouped by their parent
CREATE OR REPLACE MACRO loadouts.FileConflictsByParentGroup (db, loadoutId, removeDuplicates) AS TABLE
SELECT
  loadout_item.Parent AS GroupId,
  LIST(STRUCT_PACK(Id := "unnest".Id, LocationId := conflicts.Item2, TargetPath := conflicts.Item3)) as Items
FROM
  loadouts.FileConflicts (db, loadoutId, removeDuplicates) as conflicts
  CROSS JOIN UNNEST(conflicts)
  JOIN MDB_LOADOUTITEM (Db => db) loadout_item ON loadout_item.Id = "unnest".Id
GROUP BY
  loadout_item.Parent;
