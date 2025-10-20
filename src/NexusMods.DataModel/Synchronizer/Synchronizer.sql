-- namespace: NexusMods.DataModel.Synchronizer
CREATE SCHEMA IF NOT EXISTS synchronizer;

-- The sources of items in the loadout.
CREATE TYPE synchronizer.ItemType AS ENUM ('Loadout', 'Game', 'Deleted', 'Intrinsic');

-- All leaf loadout items with a target path
CREATE OR REPLACE MACRO synchronizer.LeafLoadoutItems (db) AS TABLE
SELECT
  loadout_item.Id,
  loadout_item.Loadout,
  loadout_item.Parent,
  loadout_item.TargetPath,
  loadout_file.Hash,
  loadout_file.Size,
  loadout_item.Disabled = FALSE
  AND loadout_item_group.Disabled = FALSE
  AND COALESCE(collection_group.Disabled, FALSE) = FALSE AS IsEnabled,
  deleted_file.Id IS NOT NULL AS IsDeleted
FROM
  MDB_LOADOUTITEMWITHTARGETPATH (Db => db) loadout_item
  LEFT JOIN MDB_LOADOUTITEMGROUP (Db => db) loadout_item_group ON loadout_item.Parent = loadout_item_group.Id
  LEFT JOIN MDB_COLLECTIONGROUP (Db => db) collection_group ON loadout_item_group.Parent = collection_group.Id
  LEFT JOIN MDB_DELETEDFILE (Db => db) deleted_file ON loadout_item.Id = deleted_file.Id
  LEFT JOIN MDB_LOADOUTFILE (Db => db) loadout_file on loadout_item.Id = loadout_file.Id;

-- All winning leaf loadout items with a target path
CREATE OR REPLACE MACRO synchronizer.WinningLeafLoadoutItem (db) AS TABLE
SELECT
  loadout_item.Loadout,
  arg_max(loadout_item.Id, coalesce(group_priority.Priority, 0)) AS Id,
  arg_max(loadout_item.Parent, coalesce(group_priority.Priority, 0)) AS Parent,
  arg_max(loadout_item.TargetPath, coalesce(group_priority.Priority, 0)) AS TargetPath,
  arg_max(loadout_item.Hash, coalesce(group_priority.Priority, 0)) AS Hash,
  arg_max(loadout_item.Size, coalesce(group_priority.Priority, 0)) AS Size,
  arg_max(loadout_item.IsEnabled, coalesce(group_priority.Priority, 0)) AS IsEnabled,
  arg_max(loadout_item.IsDeleted, coalesce(group_priority.Priority, 0)) AS IsDeleted
FROM
  synchronizer.LeafLoadoutItems (db) loadout_item
  LEFT JOIN MDB_LOADOUTITEMGROUPPRIORITY(DB => db) group_priority ON loadout_item.Parent = group_priority.Target
GROUP BY loadout_item.Loadout, loadout_item.TargetPath.Item2, loadout_item.TargetPath.Item3;

-- All the files in the overrides group
CREATE OR REPLACE MACRO synchronizer.OverrideFiles (db) AS TABLE
SELECT
  loadout_item.*
FROM
  synchronizer.LeafLoadoutItems (db) loadout_item
  LEFT JOIN MDB_LOADOUTOVERRIDESGROUP (Db => db) overrides_group ON loadout_item.Parent = overrides_group.Id
WHERE
  overrides_group.Id IS NOT NULL;

-- All winning files in loadouts
CREATE OR REPLACE MACRO synchronizer.WinningFiles(db) as TABLE
WITH all_files AS
(
  -- Game files on Layer 0
  SELECT
    Loadout,
    NULL Id,
    {Location: nma_fnv1a_hash_short('Game'), Path: Path} Path,
    Hash,
    Size,
    'Game'::synchronizer.ItemType ItemType,
    0 Layer 
  FROM file_hashes.loadout_files(db)
  UNION
  -- Loadout files on Layer 1
  SELECT
    loadout_item.Loadout,
    loadout_item.Id,
    {Location: loadout_item.TargetPath.Item2, Path: loadout_item.TargetPath.Item3} Path,
    loadout_item.Hash,
    loadout_item.Size,
    (CASE WHEN loadout_item.IsDeleted THEN 'Deleted' ELSE 'Loadout' END)::synchronizer.ItemType ItemType,
    1 Layer
  FROM synchronizer.WinningLeafLoadoutItem(db) loadout_item
    WHERE loadout_item.IsEnabled
  UNION
  -- Override files on Layer 2
  SELECT
    override_file.Loadout,
    override_file.Id,
    {Location: override_file.TargetPath.Item2, Path: override_file.TargetPath.Item3} Path,
    override_file.Hash,
    override_file.Size,
    (CASE WHEN override_file.IsDeleted THEN 'Deleted' ELSE 'Loadout' END)::synchronizer.ItemType ItemType,
    2 Layer
  FROM synchronizer.OverrideFiles(db) override_file
    WHERE override_file.IsEnabled
  UNION
  -- Intrinsic files on Layer 3
  SELECT
    Loadout,
    Null Id,
    Path,
    Null Hash,
    Null Size,
    'Intrinsic'::synchronizer.ItemType ItemType,
    3 Layer
  FROM intrinsic_files(Db=>db)
)
-- Group by loadout, path and take the winning file
SELECT
  arg_max(Id, Layer) Id,
  arg_max(Hash, Layer) Hash,
  arg_max(Size, Layer) Size,
  arg_max(ItemType, Layer) ItemType,
  Loadout,
  Path
FROM all_files
GROUP BY Loadout, Path;

-- Highest loadout item group priority
CREATE OR REPLACE MACRO synchronizer.MaxPriority (db) AS TABLE
SELECT
  item_group.Loadout,
  coalesce(max(item_group.Priority), 0) AS MaxPriority
FROM
  MDB_LOADOUTITEMGROUPPRIORITY (Db => db) item_group
GROUP BY item_group.Loadout;

-- Returns all file conflict groups
CREATE OR REPLACE MACRO synchronizer.FileConflicts (db, loadoutId, removeDuplicates) AS TABLE
SELECT
  {Location: TargetPath.Item2, Path: TargetPath.Item3} Path,
  LIST(STRUCT_PACK(Id := Id, IsEnabled := IsEnabled, IsDeleted := IsDeleted)) AS Conflicts
FROM
  synchronizer.LeafLoadoutItems (db)
WHERE Loadout = loadoutId
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
CREATE OR REPLACE MACRO synchronizer.FileConflictsByParentGroup (db, loadoutId, removeDuplicates) AS TABLE
SELECT
  loadout_item.Parent AS GroupId,
  LIST(STRUCT_PACK(Id := "unnest".Id, Location := conflicts.Path.Location, Path := conflicts.Path.Path)) as Items
FROM
  synchronizer.FileConflicts (db, loadoutId, removeDuplicates) as conflicts
  CROSS JOIN UNNEST(conflicts)
  JOIN MDB_LOADOUTITEM (Db => db) loadout_item ON loadout_item.Id = "unnest".Id
GROUP BY
  loadout_item.Parent;
