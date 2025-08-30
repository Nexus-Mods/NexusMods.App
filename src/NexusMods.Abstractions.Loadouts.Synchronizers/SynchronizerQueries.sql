-- namespace: NexusMods.Abstractions.Loadouts.Synchronizers

CREATE SCHEMA IF NOT EXISTS loadout_synchronizer;

-- Section level sorting of items
CREATE TYPE loadout_synchronizer.Section AS ENUM('Game', 'Items', 'Overrides');

CREATE TYPE loadout_synchronizer.ItemType AS ENUM('GameFile', 'File', 'Deleted');

-- Flags used in determining the state of a loadout item
CREATE TYPE loadout_synchronizer.Signature AS ENUM('DiskExists', 'PrevExists', 'LoadoutExists', 
    'DiskEqualsPrev', 'PrevEqualsLoadout', 'DiskEqualsLoadout', 
    'DiskArchived', 'PrevArchived', 'LoadoutArchived', 
    'PathIsIgnored');

-- Gets all the stock files for a the given loadout
CREATE MACRO loadout_synchronizer.GameFiles(db) AS TABLE
SELECT rels.Id, pathRel.Path, hashRel.xxHash3, hashRel.Size FROM
    (SELECT loadout.Id, unnest(list_concat(steamFiles.Files, gogFiles.Files)) AS relId 
        FROM MDB_LOADOUT(Db=>db) loadout
        LEFT JOIN MDB_GAMEINSTALLMETADATA(Db=>db) meta ON loadout.Installation = meta.Id
        -- Join to Steam and GOG files and use the store field from the metadata to determine which store to use.
        LEFT JOIN HASHES_STEAMMANIFEST() steamFiles on steamFiles.ManifestId in CAST(loadout.LocatorIds AS UBIGINT[]) AND meta.Store = 'Steam'
        LEFT JOIN HASHES_GOGBUILD() gogFiles on gogFiles.BuildId in CAST(loadout.LocatorIds AS UBIGINT[]) AND meta.Store = 'GOG') AS rels
        -- Get the path and hash for the file.    
        LEFT JOIN HASHES_PATHHASHRELATION() pathRel on pathRel.Id = rels.relId
        LEFT JOIN HASHES_HASHRELATION() hashRel on hashRel.Id = pathRel.Hash;

-- Get the enabled files in a loadout
CREATE MACRO loadout_synchronizer.EnabledFiles(db) AS TABLE
SELECT item.Id, item.Loadout, {Location: item.TargetPath.Item2, Path: item.TargetPath.Item3} as Path, file.Size, file.Hash
    FROM MDB_LOADOUTITEMWITHTARGETPATH(Db=>db) item
        LEFT JOIN MDB_LOADOUTFILE(Db=>db) file ON file.Id = item.Id
        LEFT JOIN MDB_LOADOUTITEMGROUP(Db=>db) grp on grp.Id = item.Parent
        LEFT JOIN MDB_COLLECTIONGROUP(Db=>db) col on col.Id = grp.Parent
    WHERE item.Disabled=FALSE AND grp.Disabled=false AND col.Disabled=false;


-- Winning files for loadouts
CREATE MACRO loadout_synchronizer.WinningFiles(db) AS TABLE
SELECT winners.* FROM
    -- Below we group by path, and we use the tuple of (Section, Id) to determine the winner                     
    (SELECT Loadout, Path, arg_max(Id, SortOrder) Id, arg_max(Hash, SortOrder) Hash, arg_max(Size, SortOrder) Size, max(SortOrder) SortOrder FROM (
        SELECT Loadout, Id, Hash, Path, Size, {Section: Section, Id: Id} SortOrder FROM
        -- First, we find all the enabled files
        (SELECT Loadout, Id, file.Hash, file.Path, file.Size, 'Items'::loadout_synchronizer.Section as Section FROM loadout_synchronizer.EnabledFiles(db) file
        UNION ALL
        -- Then all the game files                                                                                                                   
        SELECT Id as Loadout, NULL Id,  XxHash3 as Hash, {Location: 57681, Path:gameFiles.Path} as Path, Size, 'Game'::loadout_synchronizer.Section as Section  FROM loadout_synchronizer.GameFiles(db) gameFiles
        UNION ALL
        -- Finally, all the overrides                                                                                                           
        SELECT targetFile.Loadout, targetFile.Id, overrideFile.Hash, {Location:targetFile.TargetPath.Item2, Path:targetFile.TargetPath.Item3}, overrideFile.Size, 'Overrides'::loadout_synchronizer.Section as Section FROM MDB_LOADOUTOVERRIDESGROUP() overrideGroup
        LEFT JOIN MDB_LOADOUTITEMWITHTARGETPATH(Db=>db) targetFile ON targetFile.Parent = overrideGroup.Id
        LEFT JOIN MDB_LOADOUTFILE(Db=>db) overrideFile ON overrideFile.Id = targetFile.Id))
     -- Group them all by path (per loadout)                                                                                                                   
     GROUP BY Loadout, Path) winners
-- Remove any deleted files        
LEFT JOIN MDB_DELETEDFILE(Db=>db) deleted on deleted.Id = winners.Id
WHERE deleted.Id is NULL

-- Bitmask Maker for Signature States
--CREATE MACRO loadout_synchronizer.MakeSignature(DiskHash, PrevHash, LoadoutHash, DiskArchived, PrevArchived, LoadoutArchived) AS
--       SELECT 0 | 
