using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewModFiles;

/// <summary>
/// Helper utility methods/services around mod files.
/// </summary>
/// <remarks>
///     This is functionality lifted out of the old `View Mod Files` page, into
///     a separate helper, to make it more ready for deletion. This will be
///     reintegrated into the new `View Mod Files` page at a later date.
/// </remarks>
public static class ModFilesHelpers
{
    /// <summary>
    /// Removes files from a loadout item group matching the specified game path.
    /// The file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupIds">The <see cref="LoadoutItemGroup"/> IDs (usually mod) to remove files from</param>
    /// <param name="gamePath">The game path to match. Every file starting with this path is removed.</param>
    /// <returns>A task that completes when the files are removed.</returns>
    public static async Task RemoveFileOrFolder(IConnection connection, EntityId[] groupIds, GamePath gamePath)
    {
        var loadoutItemsToDelete = groupIds
            .Select(id => LoadGroup(connection, id))
            .SelectMany(group => group
                .Children
                .OfTypeLoadoutItemWithTargetPath()
                .Where(item => item.TargetPath.Item2.Equals(gamePath.LocationId) && item.TargetPath.Item3.StartsWith(gamePath.Path)))
            .ToArray();

        if (loadoutItemsToDelete.Length == 0)
            throw new InvalidOperationException($"Unable to find Loadout files with path `{gamePath}` in groups `{string.Join(", ", groupIds)}`");

        await DeleteFiles(connection, loadoutItemsToDelete);
    }

    /// <summary>
    /// Removes files from a loadout item group matching the specified game paths.
    /// Every file path can represent either a file or a folder; for folders, all
    /// files starting with the path are removed.
    /// </summary>
    /// <param name="connection">The connection used to MnemonicDB.</param>
    /// <param name="groupIds">The <see cref="LoadoutItemGroup"/> IDs (usually mod) to remove files from</param>
    /// <param name="gamePaths">The game path to match. All files starting with any of these paths are removed.</param>
    /// <returns>A task that completes when the files are removed.</returns>
    public static async Task RemoveFileOrFolders(IConnection connection, EntityId[] groupIds, GamePath[] gamePaths)
    {
        var loadoutItemsToDelete = groupIds
            .Select(id => LoadGroup(connection, id))
            .SelectMany(group => group
                .Children
                .OfTypeLoadoutItemWithTargetPath()
                .Where(item =>
                {
                    foreach (var path in gamePaths)
                    {
                        var matchLocationId = item.TargetPath.Item2.Equals(path.LocationId);
                        var matchPath = item.TargetPath.Item3.StartsWith(path.Path);
                        if (matchLocationId && matchPath)
                            return true;
                    }
                    
                    return false;
                }))
            .ToArray();

        if (loadoutItemsToDelete.Length == 0)
            throw new InvalidOperationException($"Unable to find Loadout files with path(s) `{gamePaths}` in groups `{string.Join(", ", groupIds)}`");

        await DeleteFiles(connection, loadoutItemsToDelete);
    }

    private static async Task DeleteFiles(IConnection connection, LoadoutItemWithTargetPath.ReadOnly[] loadoutItemsToDelete)
    {
        using var tx = connection.BeginTransaction();
        foreach (var loadoutItem in loadoutItemsToDelete)
            tx.Delete(loadoutItem, recursive: false);

        await tx.Commit();
    }

    private static LoadoutItemGroup.ReadOnly LoadGroup(IConnection connection, EntityId groupId)
    {
        var group = LoadoutItemGroup.Load(connection.Db, groupId);
        if (!group.IsValid())
            throw new InvalidOperationException($"Invalid group ID: {groupId}");

        return group;
    }
}
