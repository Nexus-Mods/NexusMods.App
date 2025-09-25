-- namespace: NexusMods.Games.FileHashes

CREATE SCHEMA IF NOT EXISTS file_hashes; 

-- Find all the gog builds that match the given game's files, and rank them by the number of files that match
CREATE MACRO file_hashes.resolve_gog_build(GameMetadataId, DefaultLanguage := 'en-US') AS TABLE
SELECT build.BuildId, ANY_VALUE(build.ProductId) AS BuildProductId, COUNT(*) matching_files, ANY_VALUE(build."version"), list_distinct(LIST(depot.ProductId)) ProductIds
FROM MDB_DISKSTATEENTRY() entry
         LEFT JOIN HASHES_HASHRELATION() hashrel on entry.Hash = hashRel.xxHash3
         LEFT JOIN HASHES_PATHHASHRELATION() pathrel on pathrel.Path = entry.Path.Item3 AND pathrel.Hash = hashrel.Id
         LEFT JOIN (SELECT Id, unnest(Files) FileId FROM HASHES_GOGMANIFEST()) manifest on pathRel.Id = manifest.FileId
         LEFT JOIN HASHES_GOGDEPOT() depot on depot.Manifest = Manifest.Id
         LEFT JOIN (SELECT Id, unnest(depots) depot, ProductId, buildId, "version" FROM HASHES_GOGBUILD()) build on depot.Id = build.Depot
WHERE entry.Game = GameMetadataId
AND DefaultLanguage in depot.Languages
GROUP BY build.BuildId
ORDER BY COUNT(*) DESC;

-- Find all the steam manifests that match the given game's files, and rank them by the number of files that match
CREATE MACRO file_hashes.resolve_steam_manifests(GameMetadataId) AS TABLE
SELECT ANY_VALUE(steam.depotId) DepotId, COUNT(*) matching_count, ANY_VALUE(steam.AppId) AppId, ANY_VALUE(steam.ManifestId)
FROM MDB_DISKSTATEENTRY() entry
         LEFT JOIN HASHES_HASHRELATION() hashrel on entry.Hash = hashRel.xxHash3
         LEFT JOIN HASHES_PATHHASHRELATION() pathrel on pathrel.Path = entry.Path.Item3 AND pathrel.Hash = hashrel.Id
         LEFT JOIN (SELECT AppId, ManifestId, DepotId, unnest(Files) File FROM HASHES_STEAMMANIFEST()) steam on steam.File = pathrel.Id
WHERE entry.Game = GameMetadataId
GROUP BY steam.ManifestId
ORDER BY COUNT(*) DESC;

-- Find all the depots (LocatorIds) for a given game. This will be the most matching depot for every AppId found in a given game folder
CREATE MACRO file_hashes.resolve_steam_depots(GameMetadataId) AS TABLE 
SELECT arg_max(ManifestId, matching_count) DepotId 
FROM file_hashes.resolve_steam_manifests(GameMetadataId) manifests
GROUP BY manifests.AppId
Having DepotId is not null;

-- ENUM of all the store names
CREATE TYPE file_hashes.Stores AS ENUM ('GOG', 'Steam');

-- gets all the loadouts, locatorids, and stores
CREATE MACRO file_hashes.loadout_locatorids(db) AS TABLE
SELECT install.Store::Stores, loadout.id Loadout, unnest(locatorIds) AS LocatorId
FROM MDB_LOADOUT(Db=>db) loadout
         LEFT JOIN MDB_GAMEINSTALLMETADATA(Db=>db) install on loadout.Installation = install.id  
    
-- gets all the paths and hashes of game files for steam loadouts
CREATE OR REPLACE MACRO file_hashes.steam_loadout_files(db) AS TABLE
SELECT files.Loadout, PathRel.Path, hashRel.xxHash3 Hash, hashRel.Size FROM
    (SELECT Loadout, ManifestId, unnest(Files) FileId
     FROM file_hashes.loadout_locatorids(db) locators
              LEFT JOIN HASHES_STEAMMANIFEST(Db=>db) manifest ON manifest.ManifestId = locators.LocatorID::UBIGINT
     WHERE locators.Store = 'Steam') files
        LEFT JOIN HASHES_PATHHASHRELATION() pathrel on files.FileId = pathrel.Id
        LEFT JOIN HASHES_HASHRELATION() hashrel on pathrel.Hash = hashrel.Id


-- gets all the paths and hashes of game files for gog loadouts
CREATE OR REPLACE MACRO file_hashes.gog_loadout_files(db) AS TABLE
WITH 
  locatorIds AS (SELECT Loadout, LocatorId::UBIGINT LocatorId
                 FROM file_hashes.loadout_locatorids(db) locators
                 WHERE locators.Store = 'GOG'), 
  builds AS (SELECT Loadout, BuildId, ProductId BaseProductId, unnest(build.Depots) DepotId
             FROM HASHES_GOGBUILD() build
             INNER JOIN locatorIds ON build.BuildId = locatorIds.LocatorId),
  validDepots AS (SELECT Id, ProductId, Manifest 
                  FROM HASHES_GOGDEPOT() 
                  WHERE Languages == [] OR 'en-US' in Languages),
  manifests AS (SELECT builds.Loadout, validDepots.Manifest 
                FROM validDepots 
                INNER JOIN builds ON validDepots.ProductId = builds.BaseProductId
                UNION
                SELECT locatorIds.Loadout, validDepots.Manifest 
                FROM validDepots 
                INNER JOIN locatorIds ON validDepots.ProductId = LocatorIds.LocatorId),
  files AS (SELECT Loadout, unnest(manifest.Files) File
            FROM manifests
            LEFT JOIN HASHES_GOGMANIFEST() manifest ON manifests.Manifest = manifest.Id)
SELECT files.Loadout, pathrel.Path, hashrel.XxHash3 Hash, hashrel.Size 
    FROM files
    LEFT JOIN HASHES_PATHHASHRELATION() pathrel ON pathrel.Id = files.File
    LEFT JOIN HASHES_HASHRELATION() hashrel ON hashrel.Id = pathrel.Hash

-- gets all the paths and hashes for game files in loadouts
CREATE MACRO file_hashes.loadout_files(db) AS TABLE
SELECT Loadout, Path, Hash, Size FROM file_hashes.gog_loadout_files(db)
UNION
SELECT Loadout, Path, Hash, Size FROM file_hashes.steam_loadout_files(db)
