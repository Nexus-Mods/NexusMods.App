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
