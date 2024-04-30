using NexusMods.Abstractions.GameLocators.Trees;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// A file tree is a tree that contains all files from a loadout, flattened into a single tree.
/// </summary>
public class FileTree : AGamePathNodeTree<File.Model>
{
    private FileTree(IEnumerable<KeyValuePair<GamePath, File.Model>> tree) : base(tree) { }

    /// <summary>
    ///     Creates a file tree from a list mod files.
    /// </summary>
    public static FileTree Create(IEnumerable<KeyValuePair<GamePath, File.Model>> items) => new(items);
}
