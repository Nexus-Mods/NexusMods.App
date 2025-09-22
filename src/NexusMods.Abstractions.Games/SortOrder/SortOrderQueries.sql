-- namespace: NexusMods.Abstractions.Games

CREATE SCHEMA IF NOT EXISTS sortorder;

-- Returns a table of all collection/loadout pairs and their most recent transactionId for a given gameId
-- TODO: listen to changes to Game files too
-- TODO: this only checks for changes to items in collections, external changes is not covered
CREATE MACRO sortorder.TrackCollectionAndLoadoutChanges(db, gameId) AS TABLE
SELECT 
    itemGroup.Parent, 
    itemGroup.Loadout,
    GREATEST(
        COALESCE(MAX(itemDatoms.T), 0),
        COALESCE(MAX(groupDatoms.T), 0),
        COALESCE(MAX(collectionDatoms.T), 0)
    ) as tx
FROM mdb_LoadoutItemGroup(Db=>db) itemGroup
JOIN mdb_LoadoutItem(Db=>db) item  on item.Parent = itemGroup.Id
JOIN mdb_Loadout(Db=>db) loadout on itemGroup.Loadout = loadout.Id
JOIN mdb_GameInstallMetadata(Db=>db) as install on loadout.Installation = install.Id
JOIN mdb_CollectionGroup(Db=>db) collection on itemGroup.Parent = collection.Id
LEFT JOIN mdb_Datoms() itemDatoms ON itemDatoms.E = item.Id 
LEFT JOIN mdb_Datoms() groupDatoms ON groupDatoms.E = itemGroup.Id 
LEFT JOIN mdb_Datoms() collectionDatoms on collectionDatoms.E = itemGroup.Parent
WHERE install.GameId = gameId
GROUP BY itemGroup.Parent, itemGroup.Loadout;
