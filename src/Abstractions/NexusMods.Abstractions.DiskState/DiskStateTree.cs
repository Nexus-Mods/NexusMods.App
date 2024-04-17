using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
public class DiskStateTree : AGamePathNodeTree<DiskStateEntry>
{
    private DiskStateTree(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree) : base(tree) { }

    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public IId LoadoutRevision { get; set; } = IdEmpty.Empty;
    
    /// <summary>
    ///     Creates a disk state from a list of files.
    /// </summary>
    public static DiskStateTree Create(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> items) => new(items);
}
