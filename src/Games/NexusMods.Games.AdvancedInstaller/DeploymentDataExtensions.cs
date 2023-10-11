using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.Exceptions;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

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
    public static void AddFolderMapping<TValue>(this DeploymentData data,
        FileTreeNode<RelativePath, TValue> folderNode, GamePath outputFolder, bool force = false)
    {
        // Check if said location is already mapped.
        // Get all of the children of the folder node.
        // Note: We assume paths in the file tree are already sanitized, e.g. use / as separator.
        var substringLength = folderNode.Path.Path.Length;
        substringLength = substringLength == 0 ? 0 : substringLength + 1;

        // if this path is non-empty add 1 for the separator.
        // this separator is guaranteed to exist if descendant files (GetAllDescendentFiles) exist.

        // TODO: Change to `CollectionsMarshal.AsSpan` after this type returns list.
        foreach (var child in folderNode.GetAllDescendentFiles())
        {
            var childPath = child.Path.Path.Substring(substringLength);
            var newPath = new GamePath(outputFolder.LocationId, outputFolder.Path.Join(childPath));
            data.AddMapping(child.Path, newPath, force);
        }
    }
}
