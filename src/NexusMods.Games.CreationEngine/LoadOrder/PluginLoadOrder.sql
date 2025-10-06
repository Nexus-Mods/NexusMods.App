-- namespace: NexusMods.Games.CreationEngine.LoadOrder

CREATE SCHEMA IF NOT EXISTS creation_engine;

CREATE MACRO creation_engine.load_order_plugin_files(db) AS TABLE
SELECT file.Loadout,
       file.Hash, 
       file.Size, 
       file.ItemType, 
       file.Id, 
       regexp_extract(file.Path.Path, '([^/]+)$') ModKey,
       grp.Id GroupId,
       grp.Name GroupName
FROM synchronizer.WinningFiles(db) file
         LEFT JOIN mdb_LoadoutItem() itm on itm.Id = file.Id
         LEFT JOIN mdb_LoadoutITemGroup() grp on grp.Id = itm.Parent
WHERE file.Path.Location = nma_fnv1a_hash_short('Game')
  AND file.Path.Path SIMILAR TO '(?i)^Data/[^/]+\.(esp|esl|esm)$';


CREATE MACRO creation_engine.plugin_sort_order(db) AS TABLE
    SELECT sortOrder.Id SortOrderId, items.ModKey, sortItem.SortIndex, items.GroupId, items.GroupName 
    FROM MDB_SORTORDER() sortOrder
    INNER JOIN creation_engine.load_order_plugin_files(null) items ON items.Loadout = sortOrder.Loadout
    INNER JOIN MDB_PLUGINSORTENTRY() sortItem ON sortItem.ParentSortOrder = sortOrder.Id AND sortItem.ModKey = items.ModKey;


       
