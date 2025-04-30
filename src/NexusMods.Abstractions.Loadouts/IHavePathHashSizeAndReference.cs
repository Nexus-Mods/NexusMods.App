using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A interface used by structs that have a hash, size and reference, such as LoadoutFiles, and DiskStateEntries.
/// </summary>
public interface IHavePathHashSizeAndReference
{
    /// <summary>
    /// The path of the file.
    /// </summary>
    public GamePath Path { get; }
    /// <summary>
    /// The hash of the file.
    /// </summary>
    public Hash Hash { get; }
    
    /// <summary>
    /// The size of the file.
    /// </summary>
    public Size Size { get; }
    
    /// <summary>
    /// The reference to the file.
    /// </summary>
    public EntityId Reference { get; }
}
