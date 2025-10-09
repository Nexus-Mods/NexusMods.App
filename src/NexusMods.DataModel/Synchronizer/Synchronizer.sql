-- namespace: NexusMods.DataModel.Synchronizer
CREATE SCHEMA IF NOT EXISTS synchronizer;

-- The sources of items in the loadout.
CREATE TYPE synchronizer.ItemType AS ENUM ('Loadout', 'Game', 'Deleted', 'Intrinsic');

-- All the enabled files in a loadout
CREATE MACRO synchronizer.EnabledFiles(db) AS TABLE
SELECT item.Loadout, item.Id,
    {Location: item.TargetPath.Item2, Path: item.TargetPath.Item3} Path,
    file.Hash,
    file.Size,
    (CASE WHEN deletedFile.Id is NOT NULL THEN 'Deleted' ELSE 'Loadout' END)::synchronizer.ItemType ItemType
FROM MDB_LOADOUTITEMWITHTARGETPATH(Db=>db) item
    INNER JOIN MDB_LOADOUTITEMGROUP(Db=>db) itemGroup ON item.Parent = itemGroup.Id
    INNER JOIN MDB_COLLECTIONGROUP(Db=>db) coll ON itemGroup.Parent = coll.Id
    LEFT JOIN MDB_DELETEDFILE(Db=>db) deletedFile ON item.Id = deletedFile.Id
    LEFT JOIN MDB_LOADOUTFILE(Db=>db) file on item.Id = file.Id
WHERE not item.Disabled and not itemGroup.Disabled and not coll.Disabled;

-- All the files in the overrides group
CREATE MACRO synchronizer.OverrideFiles(db) AS TABLE
SELECT item.Loadout, item.Id,
    {Location: item.TargetPath.Item2, Path: item.TargetPath.Item3} Path,
    file.Hash,
    file.Size,
    (CASE WHEN deletedFile.Id is NOT NULL THEN 'Deleted' ELSE 'Loadout' END)::synchronizer.ItemType ItemType
FROM MDB_LOADOUTITEMWITHTARGETPATH(Db=>db) item
    INNER JOIN MDB_LOADOUTOVERRIDESGROUP(Db=>db) itemGroup ON item.Parent = itemGroup.Id
    LEFT JOIN MDB_DELETEDFILE(Db=>db) deletedFile ON item.Id = deletedFile.Id
    LEFT JOIN MDB_LOADOUTFILE(Db=>db) file on item.Id = file.Id
WHERE not item.Disabled;

-- All winning files in loadouts
CREATE MACRO synchronizer.WinningFiles(db) as TABLE
WITH 
   allFiles AS (-- Game files on Layer 0
                SELECT Loadout, NULL Id, {Location: nma_fnv1a_hash_short('Game'), Path: Path} Path, Hash, Size, 'Game'::synchronizer.ItemType ItemType, 0 Layer 
                FROM file_hashes.loadout_files(db)
                UNION
                -- Loadout files on Layer 1
                SELECT Loadout, Id, Path, Hash, Size, ItemType, 1 Layer FROM synchronizer.EnabledFiles(db)
                UNION
                -- Override files on Layer 2
                SELECT Loadout, Id, Path, Hash, Size, ItemType, 2 Layer FROM synchronizer.OverrideFiles(db)
                UNION
                -- Intrinsic files on Layer 3
                SELECT Loadout, Null, {Location: Path.Item1, Path: Path.Item2}, Null, Null, 'Intrinsic', 3 Layer FROM intrinsic_files(Db=>db))
-- Group by loadout, path and take the winning file
SELECT Loadout, Path, arg_max(Hash, Layer) Hash, arg_max(Size, Layer) Size, arg_max(Id, Layer) Id, arg_max(ItemType, Layer) ItemType
FROM allFiles
GROUP BY Loadout, Path;       
