-- namespace: NexusMods.DataModel.Synchronizer
CREATE SCHEMA IF NOT EXISTS synchronizer;

-- The sources of items in the loadout.
CREATE TYPE synchronizer.ItemType AS ENUM ('Loadout', 'Game', 'Deleted', 'Intrinsic');

-- Gets all loadout item groups and their enabled state
CREATE OR REPLACE MACRO synchronizer.LoadoutGroups (db) AS TABLE
SELECT
  loadout_item_group.*,
  loadout_item_group.Disabled = FALSE
  AND COALESCE(collection_group.Disabled, FALSE) = FALSE AS IsEnabled
FROM
  MDB_LOADOUTITEMGROUP (Db => db) loadout_item_group
  LEFT JOIN MDB_COLLECTIONGROUP (Db => db) collection_group ON loadout_item_group.Parent = collection_group.Id;

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
    intrinsic_file.Loadout Loadout,
    Null Id,
    {Location: intrinsic_file.Path.Item1, Path: intrinsic_file.Path.Item2} Path,
    Null Hash,
    Null Size,
    'Intrinsic'::synchronizer.ItemType ItemType,
    3 Layer
  FROM intrinsic_files(Db=>db) intrinsic_file
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

-- Gets all conflicting paths
CREATE OR REPLACE MACRO synchronizer.ConflictingPaths(db) AS TABLE
SELECT
  loadout_item.Loadout AS Loadout,
  {LocationId: loadout_item.TargetPath.Item2, Path: loadout_item.TargetPath.Item3} AS Path,
  list(struct_pack(PriorityId := priority.Id, LoadoutItemId := loadout_item.Id) ORDER BY priority.Priority) AS Conflicts,
  first(struct_pack(PriorityId := priority.Id, LoadoutItemId := loadout_item.Id) ORDER BY priority.Priority DESC) AS Winner,
  list_slice(list(struct_pack(PriorityId := priority.Id, LoadoutItemId := loadout_item.Id) ORDER BY priority.Priority), 1, -2) AS Losers,
FROM
  synchronizer.LeafLoadoutItems (db) loadout_item
  LEFT JOIN MDB_LOADOUTITEMGROUPPRIORITY (Db => db) priority ON priority.Target = loadout_item.Parent
  LEFT JOIN synchronizer.LoadoutGroups (db) loadout_item_group ON loadout_item_group.Id = loadout_item.Parent
WHERE
  priority.Id IS NOT NULL AND loadout_item.IsEnabled AND loadout_item_group.IsEnabled
GROUP BY loadout_item.Loadout, loadout_item.TargetPath.Item2, loadout_item.TargetPath.Item3
HAVING count(DISTINCT loadout_item.Hash) > 1;

-- Returns all priority groups with additional per-loadout data
CREATE OR REPLACE MACRO synchronizer.PriorityGroups (db) AS TABLE
WITH
  winnersAndLosers AS (
    SELECT
      priority.Id,
      coalesce(list(conflicting_path.Winner.LoadoutItemId) FILTER (WHERE conflicting_path.Winner.PriorityId = priority.Id), []) AS WinningFiles,
      coalesce(flatten(list(list_transform(list_filter(conflicting_path.Losers, x -> x.PriorityId = priority.Id), x -> x.LoadoutItemId))), []) AS LosingFiles
    FROM
      synchronizer.ConflictingPaths(NULL) conflicting_path
      CROSS JOIN unnest(conflicting_path.Conflicts) conflict
      JOIN MDB_LOADOUTITEMGROUPPRIORITY (Db => NULL) priority ON conflict."unnest".PriorityId = priority.Id
    GROUP BY priority.Id
  )
SELECT
  priority.*,
  row_number() OVER (PARTITION BY priority.Loadout ORDER BY priority.Priority) AS Index,
  lead(priority.Id, -1, 0) OVER (PARTITION BY priority.Loadout ORDER BY priority.Priority) As Prev,
  lead(priority.Id, 1, 0) OVER (PARTITION BY priority.Loadout ORDER BY priority.Priority) As Next,
  coalesce(winnersAndLoser.WinningFiles, []) AS WinningFiles,
  coalesce(winnersAndLoser.LosingFiles, []) AS LosingFiles
FROM
  MDB_LOADOUTITEMGROUPPRIORITY (Db => db) priority
  LEFT JOIN winnersAndLosers winnersAndLoser ON winnersAndLoser.Id = priority.Id
  LEFT JOIN synchronizer.LoadoutGroups (db) loadout_item_group ON priority.Target = loadout_item_group.Id
WHERE loadout_item_group.IsEnabled;
