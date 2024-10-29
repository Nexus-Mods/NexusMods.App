using DynamicData.Kernel;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A node in the synchronization tree.
/// </summary>
public class SyncTreeNode : ISyncNode
{
    /// <summary>
    /// Path of the file in the game folder.
    /// </summary>
    public GamePath Path { get; init; }
    
    /// <summary>
    /// The state of the file on the disk.
    /// </summary>
    public Optional<DiskStateEntry.ReadOnly> Disk { get; set; }
    
    /// <summary>
    /// The previously known state of the file on the disk.
    /// </summary>
    public Optional<DiskStateEntry.ReadOnly> Previous { get; set; }
    
    /// <summary>
    /// The state of the file in the loadout.
    /// </summary>
    public Optional<Hash> LoadoutFileHash { get; set; }
    
    /// <summary>
    /// The size of the file in the loadout.
    /// </summary>
    public Optional<Size> LoadoutFileSize { get; set; }
    
    /// <summary>
    /// The ID of the file in the loadout.
    /// </summary>
    public Optional<EntityId> LoadoutFileId { get; set; }
    
    /// <summary>
    /// Sync state signature.
    /// </summary>
    public Signature Signature { get; set; }
    
    /// <summary>
    /// Actions that can be performed on this node.
    /// </summary>
    public Actions Actions { get; set; }
    
}
