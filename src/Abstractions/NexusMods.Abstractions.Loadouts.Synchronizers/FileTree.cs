using NexusMods.Abstractions.GameLocators.Trees;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// A file tree is a tree that contains all files from a loadout, flattened into a single tree.
/// </summary>
public class FileTree : AGamePathNodeTree<AModFile>
{
    private FileTree(IEnumerable<KeyValuePair<GamePath, AModFile>> tree) : base(tree) { }

    /// <summary>
    ///     Creates a file tree from a list mod files.
    /// </summary>
    public static FileTree Create(IEnumerable<KeyValuePair<GamePath, AModFile>> items) => new(items);
}
