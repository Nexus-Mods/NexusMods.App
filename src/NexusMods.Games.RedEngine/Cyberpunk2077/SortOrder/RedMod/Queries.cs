using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public static class Queries
{
    
    /// <summary>
    /// Query to check if a collection is enabled.
    /// </summary>
    // TODO: Move to central project
    public static readonly Flow<(EntityId CollectionId, bool IsCollectionEnabled)> IsCollectionEnabledFlow =
        Pattern.Create().Db(out var collectionId, CollectionGroup.IsReadOnly, out _)
            .DbOrDefault(Query.Db,collectionId, LoadoutItem.Disabled, out var collectionDisabled)
            // TODO: Update this to properly check if collection is disabled once it will possible to discriminate between Null and default(Null)
            .Project(collectionDisabled, disabled => true, out var isCollectionEnabled)
            .Return(collectionId, isCollectionEnabled);
    
    /// <summary>
    /// Query to check if a LoadoutItemGroup representing a mod is enabled, considering also the parent collection's enabled state.
    /// </summary>
    public static readonly Flow<(EntityId GroupId, bool IsModEnabled)> IsLoadoutItemGroupEnabledFlow =
        Pattern.Create().Db(out var groupId, LoadoutItem.Parent, out var collectionId)
            .Match(IsCollectionEnabledFlow, collectionId, out var collectionIsEnabled)
            .DbOrDefault(Query.Db,groupId, LoadoutItem.Disabled, out var modDisabled)
            // TODO: Update this once DB is fixed Null optional values
            .Project(modDisabled, disabled => true, out var isModEnabled)
            .Return(groupId, collectionIsEnabled ,isModEnabled)
            .Select(row => (row.Item1, row.Item2 && row.Item3)); 
    
    /// <summary>
    /// Query to check if a LoadoutItem is enabled, considering also the states of the parent group and collection.
    /// </summary>
    public static readonly Flow<(EntityId LoaodutItemId, bool IsLoadoutItemEnabled)> IsLoadoutItemEnabledFlow =
        Pattern.Create().Db(out var itemId, LoadoutItem.Parent, out var groupId)
            .Match(IsLoadoutItemGroupEnabledFlow, groupId, out var parentGroupIsEnabled)
            .DbOrDefault(Query.Db,groupId, LoadoutItem.Disabled, out var itemDisabled)
            // TODO: Update this once DB is fixed Null optional values
            .Project(itemDisabled, disabled => true, out var isItemEnabled)
            .Return(itemId, parentGroupIsEnabled ,isItemEnabled)
            .Select(row => (row.Item1, row.Item2 && row.Item3)); 
    
    
    /// <summary>
    /// Query to retrieve RedModSortableEntries for a sort order.
    /// Includes the EntityId, Key (RedMod folder name), SortIndex, ParentSortOrderId, and FolderGamePath.
    /// </summary>
    public static readonly Flow<(EntityId SortablEntityId, RelativePath Key, int SortIndex, EntityId ParentSortOrderId, GamePath FolderGamePath)> RedModSortableEntries =
        Pattern.Create()
            .Db(out var sortableEntry, SortableEntry.ParentSortOrder, out var sortOrderId)
            .Db(sortableEntry, RedModSortableEntry.RedModFolderName, out var key)
            .Db(sortableEntry, SortableEntry.SortIndex, out var sortIndex)
            // TODO: Fix path for Redmod folders
            .Project(key, fileName => new GamePath(LocationId.Game, fileName), out var gamePath)
            .Return(sortableEntry, key, sortIndex, sortOrderId, gamePath);
    
    
    /// <summary>
    /// Query to retrieve Loadout data for a specific RedMod, given a RedMod folder game path.
    /// If multiple LoadoutItems exist for the same target path, only one is returned. Enabled items are preferred.
    /// </summary>
    public static readonly Flow<(GamePath TargetPath, EntityId ParentItemGroupId,string ParentModName, bool IsEnabled)> RedModLoadoutItems =
        Pattern.Create()
            .Db(out var loadoutFile, LoadoutItemWithTargetPath.TargetPath, out var targetPath)
            .Project(targetPath, tuple => new GamePath(tuple.Item2, tuple.Item3), out var gamePath)
            // Ensure that we have a LoadoutFile and not a DeletedFile
            .Db(loadoutFile, LoadoutFile.Hash, out _)
            .Match(IsLoadoutItemEnabledFlow, loadoutFile, out var isEnabled)
            .Db(loadoutFile, LoadoutItem.Parent, out var parentItemGroup)
            .Db(parentItemGroup, LoadoutItem.Name, out var parentModName)
            // Group by targetPath
            .Return(gamePath, parentItemGroup,parentModName, isEnabled)
            .Rekey(row=> row.Item1)
            // do a select to return 1 if enabled 0 if disabled, max by that value
            .MaxBy(row => row.Item4 ? 1 : 0)
            .Select(row => row.Value);
    
    /// <summary>
    /// Query to retrieve RedMod SortableItem data for a sort order.
    /// </summary>
    public static readonly Flow<(RelativePath Key, EntityId SortableEntryId, int SortIndex, EntityId ParentSortOrderId, GamePath TargetPath, EntityId ParentItemGroupId, string ParentModName, bool IsEnabled)> RedModSortableItemsForSortOrder =
        RedModSortableEntries.Rekey(row => row.FolderGamePath)
            .LeftOuterJoin(RedModLoadoutItems.Rekey(row => row.TargetPath))
            .Select(row => (
                row.Value.Item1.Key, row.Value.Item1.SortablEntityId, row.Value.Item1.SortIndex, row.Value.Item1.ParentSortOrderId,
                row.Value.Item2.TargetPath, row.Value.Item2.ParentItemGroupId, row.Value.Item2.ParentModName, row.Value.Item2.IsEnabled));
    
}
