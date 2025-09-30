-- namespace: NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder

CREATE SCHEMA IF NOT EXISTS redmod;

-- Returns the RedMod Sort Order Items for a given Sort Order Id
CREATE MACRO redmod.RedModSortOrderItems(db, sortOrderId) AS TABLE
SELECT s.RedModFolderName, s.SortIndex, s.Id
FROM mdb_RedModSortOrderItem(Db=>db) s
WHERE s.ParentSortOrder = sortOrderId
ORDER BY s.SortIndex;


-- Return loadout mod groups that contain a `mods/<folderName>/info.json` file for a given loadout
CREATE MACRO redmod.LoadoutRedModGroups(db, loadoutId, gameLocationId) AS TABLE
SELECT
    regexp_extract(file.TargetPath.Item3, '^mods\/([^\/]+)\/info\.json$', 1, 'i') AS ModFolderName,
    enabledState.IsEnabled AS IsEnabled,
    groupItem.Name AS ModName,
    groupItem.Id AS ModGroupId
FROM mdb_LoadoutItemWithTargetPath(Db=>db) as file
JOIN loadouts.LoadoutItemIsEnabled(db, loadoutId) as enabledState ON file.Id = enabledState.Id
JOIN mdb_LoadoutItemGroup(Db=>db) as groupItem ON file.Parent = groupItem.Id
WHERE file.TargetPath.Item1 = loadoutId
    AND file.TargetPath.Item2 = gameLocationId
    AND ModFolderName != '';


-- Return winning loadout red mod groups in case of multiple mods containing the same redmod folder
-- TODO: Update ranking logic to use better criteria than most recently created ModGroupId
CREATE MACRO redmod.WinningLoadoutRedModGroups(db, loadoutId, gameLocationId) AS TABLE
SELECT
    ModFolderName,
    IsEnabled,
    ModName,
    ModGroupId
FROM (
         SELECT
             matchingMods.*,
             ROW_NUMBER() OVER (
                            PARTITION BY matchingMods.ModFolderName
                            ORDER BY matchingMods.IsEnabled DESC, matchingMods.ModGroupId DESC
                        ) AS ranking
         FROM redmod.LoadoutRedModGroups(db, loadoutId, gameLocationId) AS matchingMods
     ) ranked
WHERE ranking = 1;


-- Return the RedMod Sort Order for a given loadout including the loadout data
CREATE MACRO redmod.RedModSortOrderWithLoadoutData(db, sortOrderId, loadoutId, gameLocationId) AS TABLE
SELECT 
    sortItem.RedModFolderName, 
    sortItem.SortIndex,
    sortItem.Id, 
    loadoutData.IsEnabled, 
    loadoutData.ModName, 
    loadoutData.ModGroupId
FROM mdb_RedModSortOrderItem(Db=>db) sortItem
LEFT OUTER JOIN redmod.WinningLoadoutRedModGroups(db, loadoutId, gameLocationId) loadoutData on sortItem.RedModFolderName = loadoutData.ModFolderName
WHERE sortItem.ParentSortOrder = sortOrderId
ORDER BY sortItem.SortIndex;
