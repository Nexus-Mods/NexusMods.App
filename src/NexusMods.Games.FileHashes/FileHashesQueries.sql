-- namespace: NexusMods.Games.FileHashes
CREATE SCHEMA IF NOT EXISTS file_hashes;

-- ENUM of all the store names
CREATE TYPE file_hashes.Stores AS ENUM ('Unknown', 'GOG', 'Steam', 'EA Desktop', 'Epic Games Store', 'Origin', 'Xbox Game Pass', 'Manually Added');

-- Find all the gog builds that match the given game's files, and rank them by the number of files that match
CREATE MACRO file_hashes.resolve_gog_build(GameMetadataId, DefaultLanguage := 'en-US') AS TABLE
SELECT build.BuildId, ANY_VALUE(build.ProductId) AS BuildProductId, COUNT(*) matching_files, ANY_VALUE(build."version"), list_distinct(LIST(depot.ProductId)) ProductIds
FROM MDB_DISKSTATEENTRY() entry
         LEFT JOIN MDB_HASHRELATION(DBName=>"hashes") hashrel on entry.Hash = hashRel.xxHash3
         LEFT JOIN MDB_PATHHASHRELATION(DBName=>"hashes") pathrel on pathrel.Path = entry.Path.Item3 AND pathrel.Hash = hashrel.Id
         LEFT JOIN (SELECT Id, unnest(Files) FileId FROM MDB_GOGMANIFEST(DBName=>"hashes")) manifest on pathRel.Id = manifest.FileId
         LEFT JOIN MDB_GOGDEPOT(DBName=>"hashes") depot on depot.Manifest = Manifest.Id
         LEFT JOIN (SELECT Id, unnest(depots) depot, ProductId, buildId, "version" FROM MDB_GOGBUILD(DBName=>"hashes")) build on depot.Id = build.Depot
WHERE entry.Game = GameMetadataId
AND DefaultLanguage in depot.Languages
GROUP BY build.BuildId
ORDER BY COUNT(*) DESC;

-- Find all the steam manifests that match the given game's files, and rank them by the number of files that match
CREATE MACRO file_hashes.resolve_steam_manifests(GameMetadataId) AS TABLE
SELECT ANY_VALUE(steam.depotId) DepotId, COUNT(*) matching_count, ANY_VALUE(steam.AppId) AppId, ANY_VALUE(steam.ManifestId)
FROM MDB_DISKSTATEENTRY() entry
         LEFT JOIN MDB_HASHRELATION(DBName=>"hashes") hashrel on entry.Hash = hashRel.xxHash3
         LEFT JOIN MDB_PATHHASHRELATION(DBName=>"hashes") pathrel on pathrel.Path = entry.Path.Item3 AND pathrel.Hash = hashrel.Id
         LEFT JOIN (SELECT AppId, ManifestId, DepotId, unnest(Files) File FROM MDB_STEAMMANIFEST(DBName=>"hashes")) steam on steam.File = pathrel.Id
WHERE entry.Game = GameMetadataId
GROUP BY steam.ManifestId
ORDER BY COUNT(*) DESC;

-- Find all the depots (LocatorIds) for a given game. This will be the most matching depot for every AppId found in a given game folder
CREATE MACRO file_hashes.resolve_steam_depots(GameMetadataId) AS TABLE 
SELECT arg_max(ManifestId, matching_count) DepotId 
FROM file_hashes.resolve_steam_manifests(GameMetadataId) manifests
GROUP BY manifests.AppId
Having DepotId is not null;

-- gets all the loadouts, locatorids, and stores
CREATE MACRO file_hashes.loadout_locatorids(db) AS TABLE
SELECT install.Store::file_hashes.Stores Store, loadout.id Loadout, unnest(locatorIds) AS LocatorId
FROM MDB_LOADOUT(Db=>db) loadout
         LEFT JOIN MDB_GAMEINSTALLMETADATA(Db=>db) install on loadout.Installation = install.id;  
    
-- gets all the paths and hashes of game files for steam loadouts
CREATE OR REPLACE MACRO file_hashes.steam_loadout_files(db) AS TABLE
SELECT files.Loadout, files.FileId PathId FROM
    (SELECT Loadout, ManifestId, unnest(Files) FileId
     FROM file_hashes.loadout_locatorids(db) locators
              LEFT JOIN MDB_STEAMMANIFEST(DbName=>'hashes') manifest ON manifest.ManifestId = locators.LocatorID::UBIGINT
     WHERE locators.Store = 'Steam') files;


-- gets all the paths and hashes of game files for gog loadouts
CREATE OR REPLACE MACRO file_hashes.gog_loadout_files(db) AS TABLE
WITH 
  -- GOG locatorIds can contain a mix of BuildIds and DLC ProductIds
  locatorIds AS (SELECT Loadout, LocatorId::UBIGINT LocatorId
                 FROM file_hashes.loadout_locatorids(db) locators
                 WHERE locators.Store = 'GOG'), 
  builds AS (SELECT Loadout, BuildId, ProductId BuildProductId, unnest(build.Depots) DepotId
             FROM MDB_GOGBUILD(DBName=>"hashes") build
             INNER JOIN locatorIds ON build.BuildId = locatorIds.LocatorId),
  validDepots AS (SELECT Id, ProductId, Manifest ManifestId 
                  FROM MDB_GOGDEPOT(DBName=>"hashes")
                  WHERE Languages == [] OR 'en-US' in Languages),
  buildDepots AS (SELECT builds.Loadout, builds.DepotId, validDepots.ProductId DepotProductId, builds.BuildProductId, validDepots.ManifestId
                  FROM builds
                  JOIN validDepots on validDepots.Id = builds.DepotId),
  manifests AS (-- Depots for the base game product
                SELECT buildDepots.Loadout, buildDepots.ManifestId 
                FROM buildDepots 
                WHERE buildDepots.DepotProductId = buildDepots.BuildProductId
                UNION
                -- Depots for DLC products
                SELECT buildDepots.Loadout, buildDepots.ManifestId
                FROM buildDepots 
                JOIN locatorIds dlcProducts ON dlcProducts.Loadout = buildDepots.Loadout AND buildDepots.DepotProductId = dlcProducts.LocatorId),
  files AS (SELECT Loadout, unnest(manifest.Files) File
            FROM manifests
            LEFT JOIN MDB_GOGMANIFEST(DBName=>"hashes") manifest ON manifests.ManifestId = manifest.Id)
SELECT files.Loadout, files.File PathId FROM files;

-- gets all the paths and hashes for game files in loadouts
CREATE MACRO file_hashes.loadout_files(db) AS TABLE
WITH 
       relations AS (SELECT pathRel.Id, pathRel.Path, hashRel.xxHash3 Hash, hashRel.Size 
                  FROM MDB_PathHashRelation(DBName=>"hashes") pathRel
                  INNER JOIN MDB_hashrelation(DBName=>"hashes") hashRel ON pathRel.Hash = hashRel.Id),
       files AS (SELECT Loadout, PathId FROM file_hashes.gog_loadout_files(db)
              UNION
              SELECT Loadout, PathId FROM file_hashes.steam_loadout_files(db))
SELECT files.Loadout, relations.Path, relations.Hash, relations.Size FROM files
INNER JOIN relations ON files.PathId = relations.Id;

       
       
