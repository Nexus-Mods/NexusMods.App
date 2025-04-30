
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// The type of item this is.
/// </summary>
public enum LoadoutSourceItemType : byte
{
    /// <summary>
    /// This item is attached to a specific loadout item
    /// </summary>
    Loadout,
    
    /// <summary>
    /// This is a game file
    /// </summary>
    Game,
    
    /// <summary>
    /// This is a deleted file
    /// </summary>
    Deleted,
}

public readonly record struct LoadoutSourceItem
{
    /// <summary>
    /// The path to the file.
    /// </summary>
    public required GamePath Path { get; init; }

    /// <summary>
    /// The size of the file.
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The known hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The Loadout item this item is associated with, if any.
    /// </summary>
    public Optional<EntityId> SourceId { get; init; }
    
    /// <summary>
    /// The type of item this is, a game file, loadout item, etc.
    /// </summary>
    public LoadoutSourceItemType Type { get; init; }
}
