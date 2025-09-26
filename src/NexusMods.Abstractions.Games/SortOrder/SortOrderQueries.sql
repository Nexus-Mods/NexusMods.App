-- namespace: NexusMods.Abstractions.Games

CREATE SCHEMA IF NOT EXISTS sortorder;

-- Returns a table of all LoadoutItemWithTargetPath in for a given GameId
-- TODO: listen to changes to Game files too
CREATE MACRO sortorder.TrackLoadoutItemChanges(db, gameId) AS TABLE
SELECT
    item.Id as ItemId,
    item.Parent as GroupId,
    itemGroup.Parent as CollectionId,
    item.Loadout as LoadoutId
FROM mdb_LoadoutItemWithTargetPath(Db=>db) item
JOIN mdb_LoadoutItemGroup(Db=>db) itemGroup on item.Parent = itemGroup.Id
JOIN mdb_Loadout(Db=>db) loadout on item.Loadout = loadout.Id
JOIN mdb_GameInstallMetadata(Db=>db) as installation on loadout.Installation = installation.Id
WHERE installation.GameId = gameId;
