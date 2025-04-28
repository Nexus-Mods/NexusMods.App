using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A pairing of a path and a sync node part.
/// </summary>
public readonly record struct PathPartPair(GamePath Path, SyncNodePart Part);

/// <summary>
/// A pairing of a path and a sync tree node.
/// </summary>
public readonly record struct PathNodePair(GamePath Path, SyncNode Node);

/// <summary>
/// A grouping of (optional) entityId, hash, size. 8bytes for each, so 24 bytes total.
/// </summary>
public readonly struct SyncNodePart
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public SyncNodePart()
    {
        EntityId = EntityId.From(0);
        Hash = Hash.Zero;
        Size = Size.Zero;
    }
    
    /// <summary>
    /// An optional entity ID that links to where this data came from. For loadout parts
    /// this will be the loadout item. For DiskState parts this will be the disk state entry.
    /// </summary>
    public EntityId EntityId { get; init; }
    
    /// <summary>
    /// The xxHash3 hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The size of the file.
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The last modified time of the file in UTC ticks.
    /// </summary>
    public required long LastModifiedTicks { get; init; }

    /// <summary>
    /// True if the entity ID has a value.
    /// </summary>
    public bool HaveEntityId => EntityId.Value != 0;
}

/// <summary>
/// A node in the synchronization tree. 24 bytes for each part, and two ushorts for the other fields. So 76 bytes total.
/// </summary>
public record struct SyncNode
{
    /// <summary>
    /// The current disk state
    /// </summary>
    public SyncNodePart Disk { get; set; }

    /// <summary>
    /// The previously applied state
    /// </summary>
    public SyncNodePart Previous { get; set; }
    
    /// <summary>
    /// The loadout state
    /// </summary>
    public SyncNodePart Loadout { get; set; }

    /// <summary>
    /// True if the disk state is present.
    /// </summary>
    public bool HaveDisk => Disk.Hash != Hash.Zero;
    
    /// <summary>
    /// True if the previous state is present.
    /// </summary>
    public bool HavePrevious => Previous.Hash != Hash.Zero;
    
    /// <summary>
    /// True if the loadout state is present.
    /// </summary>
    public bool HaveLoadout => Loadout.Hash != Hash.Zero;
    
    /// <summary>
    /// The type of thing that the data from the loadout came from. Game state, deleted file, loadout file, etc.
    /// </summary>
    public LoadoutSourceItemType SourceItemType { get; set; }
    
    /// <summary>
    /// Sync state signature.
    /// </summary>
    public Signature Signature { get; set; }
    
    /// <summary>
    /// Actions that can be performed on this node.
    /// </summary>
    public Actions Actions { get; set; }
}
