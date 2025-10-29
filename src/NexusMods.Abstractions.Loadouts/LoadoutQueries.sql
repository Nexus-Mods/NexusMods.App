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

-- Returns all collection download rules
CREATE OR REPLACE MACRO loadouts.CollectionDownloadRules (db) AS TABLE
SELECT
  download_rule.*,
  source_download.Id AS SourceId,
  other_download.Id AS OtherId,
  source_download.CollectionRevision AS RevisionId
FROM
  MDB_COLLECTIONDOWNLOADRULES (Db => db) download_rule
  LEFT JOIN MDB_COLLECTIONDOWNLOAD (Db => db) source_download ON download_rule.Source = source_download.Id
  LEFT JOIN MDB_COLLECTIONDOWNLOAD (Db => db) other_download ON download_rule.Other = other_download.Id
WHERE
  source_download.Id IS NOT NULL
  AND other_download.Id IS NOT NULL;

-- Returns collection loadout items with their resolved collection rules
CREATE OR REPLACE MACRO loadouts.CollectionRulesOnItems (db) AS TABLE
SELECT
  source_loadout_group.Id AS Source,
  other_loadout_group.Id AS Other,
  source_loadout_group.Parent AS Parent,
  download_rule.RuleType AS RuleType
FROM
  MDB_NEXUSCOLLECTIONITEMLOADOUTGROUP (Db => db) source_loadout_group
  LEFT JOIN loadouts.CollectionDownloadRules (db) download_rule ON download_rule.Source = source_loadout_group.Download
  LEFT JOIN MDB_NEXUSCOLLECTIONITEMLOADOUTGROUP (Db => db) other_loadout_group ON download_rule.Other = other_loadout_group.Download
  AND source_loadout_group.Parent = other_loadout_group.Parent
WHERE
  download_rule.Id IS NOT NULL
  AND other_loadout_group.Id IS NOT NULL;
