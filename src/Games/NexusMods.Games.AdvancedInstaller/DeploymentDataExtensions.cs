using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.AdvancedInstaller.Exceptions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
///     Extension methods for the <see cref="DeploymentData" /> class which involve external types.
/// </summary>
public static class DeploymentDataExtensions
{
    /// <summary>
    ///     Maps all of the children of the given path onto the given output folder.
    /// </summary>
    /// <param name="data">The instance of <see cref="DeploymentData" />.</param>
    /// <param name="folderNode">The relative path of the source file within the mod archive.</param>
    /// <param name="outputFolder">The folder where the file should be placed in one of the game directories.</param>
    /// <param name="force">
    ///     If this is set to true, re-mapping of items is permitted, and remapping will simply unmap existing item.
    ///     Otherwise <see cref="MappingAlreadyExistsException"/> will be thrown.
    /// </param>
    /// <returns>True if the mapping was added successfully, false if the key already exists.</returns>
    /// <exception cref="MappingAlreadyExistsException">If the mapping already exists, unless <see cref="force"/> is specified.</exception>
    public static void AddFolderMapping(this DeploymentData data,
        KeyedBox<RelativePath, ModFileTree> folderNode, GamePath outputFolder, bool force = false)
    {
        // Check if said location is already mapped.
        // Get all of the children of the folder node.
        // Note: We assume paths in the file tree are already sanitized, e.g. use / as separator.
        var substringLength = folderNode.Path().Path.Length;
        substringLength = substringLength == 0 ? 0 : substringLength + 1;

        // if this path is non-empty add 1 for the separator.
        // this separator is guaranteed to exist if descendant files (GetAllDescendentFiles) exist.

        // TODO: Change to `CollectionsMarshal.AsSpan` after this type returns list.
        foreach (var child in folderNode.GetFiles())
        {
            var relativePath = child.Path();
            var childPath = relativePath.Path[substringLength..];
            var newPath = new GamePath(outputFolder.LocationId, outputFolder.Path.Join(childPath));
            data.AddMapping(relativePath, newPath, force);
        }
    }

    /// <summary>
    ///     Removes the mappings of all of the children of the given node.
    /// </summary>
    /// <param name="data">The instance of <see cref="DeploymentData" />.</param>
    /// <param name="folderNode">The relative path of the source file within the mod archive.</param>
    /// <returns>True if the mapping was removed successfully, false if the key doesn't exist.</returns>
    public static void RemoveFolderMapping(this DeploymentData data,
        KeyedBox<RelativePath, ModFileTree> folderNode)
    {
        foreach (var child in folderNode.GetFiles())
            data.RemoveMapping(child.Path());
    }
}
