using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.App.UI.Pages.LoadoutGroupFilesPage;

/// <summary>
/// Helper utility methods/services around mod files.
/// </summary>
/// <remarks>
///     This is functionality lifted out of the old `View Mod Files` page, into
///     a separate helper, to make it more ready for deletion. This will be
///     reintegrated into the new `View Mod Files` page at a later date.
/// </remarks>
public static class LoadoutItemGroupHelpers
{
    /// <summary>
    /// Removes files from a loadout item group matching the specified game path.
    /// The file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupIds">The <see cref="LoadoutItemGroup"/> IDs (usually mod) to remove files from</param>
    /// <param name="gamePath">The game path to match. Every file starting with this path is removed.</param>
    /// <param name="requireAllGroups">If true, throws InvalidOperationException when any group is missing. If false, continues with available groups.</param>
    /// <returns>A task that completes with the operation status when the files are removed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when requireAllGroups is true and any group is missing</exception>
    public static async Task<GroupOperationStatus> RemoveFileOrFolder(IConnection connection, LoadoutItemGroupId[] groupIds, GamePath gamePath, bool requireAllGroups = true)
    {
        var (validGroups, missingGroups) = LoadAllGroups(connection, groupIds, requireAllGroups);
        
        if (validGroups.Length == 0)
            return GroupOperationStatus.NoItemsDeleted;
            
        var loadoutItemsToDelete = validGroups
            .SelectMany(group => group
                .Children
                .OfTypeLoadoutItemWithTargetPath()
                .Where(item => PathMatches(item, gamePath)))
            .ToArray();

        if (loadoutItemsToDelete.Length == 0)
            return GroupOperationStatus.NoItemsDeleted;

        await DeleteFiles(connection, loadoutItemsToDelete);
        return missingGroups.Length > 0 ? GroupOperationStatus.MissingGroups : GroupOperationStatus.Success;
    }

    /// <summary>
    /// Removes files from a loadout item group matching the specified game paths.
    /// Every file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupIds">The <see cref="LoadoutItemGroup"/> IDs (usually mod) to remove files from</param>
    /// <param name="gamePaths">The game path to match. All files starting with any of these paths are removed.</param>
    /// <param name="requireAllGroups">If true, throws InvalidOperationException when any group is missing. If false, continues with available groups.</param>
    /// <returns>A task that completes with the operation status when the files are removed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when requireAllGroups is true and any group is missing</exception>
    public static async Task<GroupOperationStatus> RemoveFilesOrFolders(IConnection connection, LoadoutItemGroupId[] groupIds, GamePath[] gamePaths, bool requireAllGroups = true)
    {
        var (validGroups, missingGroups) = LoadAllGroups(connection, groupIds, requireAllGroups);
        
        if (validGroups.Length == 0)
            return GroupOperationStatus.NoItemsDeleted;
            
        var loadoutItemsToDelete = validGroups
            .SelectMany(group => group
                .Children
                .OfTypeLoadoutItemWithTargetPath()
                .Where(item => PathMatchesAny(item, gamePaths)))
            .ToArray();

        if (loadoutItemsToDelete.Length == 0)
            return GroupOperationStatus.NoItemsDeleted;

        await DeleteFiles(connection, loadoutItemsToDelete);
        return missingGroups.Length > 0 ? GroupOperationStatus.MissingGroups : GroupOperationStatus.Success;
    }

    /// <summary>
    /// Finds a LoadoutItemWithTargetPath matching the specified game path within the given loadout item groups.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupIds">The <see cref="LoadoutItemGroup"/> IDs (usually mod) to search within.</param>
    /// <param name="gamePath">The game path to match. Searches for an exact match.</param>
    /// <param name="requireAllGroups">If true, throws <see cref="InvalidOperationException"/> when any group is missing. If false, continues with available groups.</param>
    /// <returns>The matching <see cref="LoadoutItemWithTargetPath.ReadOnly"/> if found; otherwise, null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when requireAllGroups is true and any group is missing.</exception>
    public static LoadoutItemWithTargetPath.ReadOnly? FindMatchingFile(IConnection connection, LoadoutItemGroupId[] groupIds, GamePath gamePath, bool requireAllGroups = true)
    {
        var (validGroups, _) = LoadAllGroups(connection, groupIds, requireAllGroups);
        
        if (validGroups.Length == 0)
            return null;
            
        foreach (var group in validGroups)
        {
            foreach (var item in group.Children.OfTypeLoadoutItemWithTargetPath())
            {
                if (item.TargetPath == gamePath)
                    return item;
            }
        }
        
        return null;
    }

    private static bool PathMatches(LoadoutItemWithTargetPath.ReadOnly item, GamePath path)
    {
        return item.TargetPath.Item2.Equals(path.LocationId) && item.TargetPath.Item3.StartsWith(path.Path);
    }

    private static bool PathMatchesAny(LoadoutItemWithTargetPath.ReadOnly item, GamePath[] paths)
    {
        foreach (var path in paths)
        {
            if (PathMatches(item, path))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Loads all specified <see cref="LoadoutItemGroup"/>(s) from the database.
    /// </summary>
    /// <param name="connection">Database connection to use for loading</param>
    /// <param name="groupIds">Array of group IDs to load</param>
    /// <param name="requireAllGroups">If true, throws an exception if any groups are missing</param>
    /// <returns>Tuple containing (valid groups array, missing group IDs array)</returns>
    public static (LoadoutItemGroup.ReadOnly[] validGroups, LoadoutItemGroupId[] missingGroups) LoadAllGroups(IConnection connection, Span<LoadoutItemGroupId> groupIds, bool requireAllGroups)
    {
        var validGroups = new List<LoadoutItemGroup.ReadOnly>();
        var missingGroups = new List<LoadoutItemGroupId>();

        foreach (var groupId in groupIds)
        {
            var group = LoadoutItemGroup.Load(connection.Db, groupId);
            if (group.IsValid())
                validGroups.Add(group);
            else
                missingGroups.Add(groupId);
        }

        if (requireAllGroups && missingGroups.Count > 0)
            throw new InvalidOperationException($"Missing group IDs: {string.Join(", ", missingGroups)}");

        return (validGroups.ToArray(), missingGroups.ToArray());
    }

    private static async Task DeleteFiles(IConnection connection, LoadoutItemWithTargetPath.ReadOnly[] loadoutItemsToDelete)
    {
        using var tx = connection.BeginTransaction();
        foreach (var loadoutItem in loadoutItemsToDelete)
            tx.Delete(loadoutItem, recursive: false);

        await tx.Commit();
    }
    
    /// <summary>
    /// Gets the first valid <see cref="LoadoutItemGroup"/> from the specified group IDs.
    /// </summary>
    /// <param name="connection">Database connection to use for loading</param>
    /// <param name="groupIds">Array of group IDs to check</param>
    /// <returns>The first valid group found, or null if none are valid</returns>
    public static LoadoutItemGroup.ReadOnly? GetFirstValidGroup(IConnection connection, Span<LoadoutItemGroupId> groupIds)
    {
        foreach (var groupId in groupIds)
        {
            var group = LoadoutItemGroup.Load(connection.Db, groupId);
            if (group.IsValid())
                return group;
        }

        return null;
    }
    
    /// <summary>
    /// Gets the first valid <see cref="LoadoutItemGroup"/> from the specified group IDs.
    /// </summary>
    /// <param name="connection">Database connection to use for loading</param>
    /// <param name="groupIds">Array of group IDs to check</param>
    /// <returns>The first valid group found, or null if none are valid</returns>
    public static LoadoutItemGroup.ReadOnly? GetFirstValidGroup(IConnection connection, Span<EntityId> groupIds)
    {
        foreach (var groupId in groupIds)
        {
            var group = LoadoutItemGroup.Load(connection.Db, groupId);
            if (group.IsValid())
                return group;
        }

        return null;
    }

    /// <summary>
    /// Result status for group operations
    /// </summary>
    public enum GroupOperationStatus
    {
        /// <summary>
        /// No items were deleted from any of the groups.
        /// </summary>
        NoItemsDeleted,
        
        /// <summary>
        /// Some groups were missing but delete operation completed for existing ones
        /// </summary>
        MissingGroups,
        
        /// <summary>
        /// All items from all groups were deleted successfully
        /// </summary>
        Success,
    }
}
