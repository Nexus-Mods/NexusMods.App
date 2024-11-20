using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;

namespace NexusMods.Abstractions.Loadouts.Files.Diff;

/// <summary>
/// A tree that files and their change states
/// </summary>
public class FileDiffTree : AGamePathNodeTree<DiskDiffEntry>
{
    private FileDiffTree(IEnumerable<KeyValuePair<GamePath, DiskDiffEntry>> tree) : base(tree) { }
    
    /// <summary>
    /// Creates a diff tree from a list of files and change states
    /// </summary>
    public static FileDiffTree Create(IEnumerable<KeyValuePair<GamePath, DiskDiffEntry>> items) => new(items);
}
