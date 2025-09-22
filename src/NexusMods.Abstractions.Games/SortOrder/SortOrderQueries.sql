-- namespace: NexusMods.Abstractions.Games

CREATE SCHEMA IF NOT EXISTS sortorder;

-- Returns a table of all collection/loadout pairs and their most recent transactionId for a given gameId
-- TODO: listen to changes to Game files too
-- TODO: this only checks for changes to items in collections, external changes is not covered
CREATE MACRO sortorder.TrackCollectionAndLoadoutChanges(db, gameId) AS TABLE
SELECT itemGroup.Parent, itemGroup.Loadout, MAX(d.T) as tx
FROM mdb_LoadoutItemGroup(Db=>db) itemGroup
JOIN mdb_LoadoutItem(Db=>db) item  on item.Parent = itemGroup.Id
JOIN mdb_Loadout(Db=>db) loadout on itemGroup.Loadout = loadout.Id
JOIN mdb_GameInstallMetadata(Db=>db) as install on loadout.Installation = install.Id
JOIN mdb_CollectionGroup(Db=>db) collection on itemGroup.Parent = collection.Id
JOIN mdb_Datoms() d ON d.E = item.Id OR d.E = itemGroup.Id OR d.E = itemGroup.Parent
WHERE install.GameId = gameId
GROUP BY itemGroup.Parent, itemGroup.Loadout;
