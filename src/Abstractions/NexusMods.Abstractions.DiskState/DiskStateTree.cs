using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
public class DiskStateTree : AGamePathNodeTree<DiskStateEntry>
{
    private DiskStateTree(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree) : base(tree) { }

    /// <summary>
    /// The associated tx id for this disk state.
    /// </summary>
    public TxId TxId { get; set; }
    
    /// <summary>
    /// The associated loadout id for this disk state.
    /// </summary>
    public EntityId LoadoutId { get; set; }
    
    /// <summary>
    ///     Creates a disk state from a list of files.
    /// </summary>
    public static DiskStateTree Create(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> items) => new(items);
}
