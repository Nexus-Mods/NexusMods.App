using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewModFiles;

/// <summary>
/// Helper utility methods/services around mod files.
/// </summary>
public static class ModFilesHelpers
{
    /// <summary>
    /// Removes files from a loadout item group matching the specified game path.
    /// The file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupId">The <see cref="LoadoutItemGroup"/> ID (usually mod) to remove files from</param>
    /// <param name="gamePath">The game path to match. Every file starting with this path is removed.</param>
    /// <returns>A task that completes when the files are removed. Contains latest state of the group where the files were removed.</returns>
    public static async Task<LoadoutItemGroup.ReadOnly> RemoveFileOrFolder(IConnection connection, EntityId groupId, GamePath gamePath)
    {
        var group = LoadGroup(connection, groupId);
        var loadoutItemsToDelete = group
            .Children
            .OfTypeLoadoutItemWithTargetPath()
            .Where(item => item.TargetPath.Item2.Equals(gamePath.LocationId) && item.TargetPath.Item3.StartsWith(gamePath.Path))
            .ToArray();

        if (loadoutItemsToDelete.Length == 0)
            throw new InvalidOperationException($"Unable to find Loadout files with path `{gamePath}` in group `{group.AsLoadoutItem().Name}`");

        return await DeleteFilesAndReturnNewerGroup(connection, loadoutItemsToDelete, group);
    }

    /// <summary>
    /// Removes files from a loadout item group matching the specified game paths.
    /// Every file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupId">The <see cref="LoadoutItemGroup"/> ID (usually mod) to remove files from</param>
    /// <param name="gamePaths">The game path to match. All files starting with any of these paths are removed.</param>
    /// <returns>A task that completes when the files are removed. Contains latest state of the group where the files were removed.</returns>
    public static async Task<LoadoutItemGroup.ReadOnly> RemoveFileOrFolders(IConnection connection, EntityId groupId, GamePath[] gamePaths)
    {
        var group = LoadGroup(connection, groupId);
        var loadoutItemsToDelete = group
            .Children
            .OfTypeLoadoutItemWithTargetPath()
            .Where(item =>
            {
                // Match first path.
                // If this is a file, match direct, else match via parent folder.
                foreach (var path in gamePaths)
                {
                    var matchLocationId = item.TargetPath.Item2.Equals(path.LocationId);
                    var matchPath = item.TargetPath.Item3.StartsWith(path.Path);
                    if (matchLocationId && matchPath)
                        return true;
                }
                
                return false;
            }).ToArray();

        if (loadoutItemsToDelete.Length == 0)
            throw new InvalidOperationException($"Unable to find Loadout files with path(s) `{gamePaths}` in group `{group.AsLoadoutItem().Name}`");

        return await DeleteFilesAndReturnNewerGroup(connection, loadoutItemsToDelete, group);
    }
    private static async Task<LoadoutItemGroup.ReadOnly> DeleteFilesAndReturnNewerGroup(IConnection connection, LoadoutItemWithTargetPath.ReadOnly[] loadoutItemsToDelete, LoadoutItemGroup.ReadOnly group)
    {
        using var tx = connection.BeginTransaction();
        foreach (var loadoutItem in loadoutItemsToDelete)
            tx.Delete(loadoutItem, recursive: false);

        await tx.Commit();
        return group.Rebase();
    }

    private static LoadoutItemGroup.ReadOnly LoadGroup(IConnection connection, EntityId groupId)
    {
        var group = LoadoutItemGroup.Load(connection.Db, groupId);
        if (!group.IsValid())
            throw new InvalidOperationException($"Invalid group ID: {groupId}");

        return group;
    }
}
