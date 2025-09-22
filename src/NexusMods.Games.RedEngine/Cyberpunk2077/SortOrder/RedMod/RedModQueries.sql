-- namespace: NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder

CREATE SCHEMA IF NOT EXISTS redmod;

-- Returns the RedMod Sort Order Items for a given Sort Order Id
CREATE MACRO redmod.RedModSortOrderItems(db, sortOrderId) AS TABLE
SELECT s.RedModFolderName, s.SortIndex, s.Id
FROM mdb_RedModSortOrderItem(Db=>db) s
WHERE s.ParentSortOrder = sortOrderId
ORDER BY s.SortIndex;


