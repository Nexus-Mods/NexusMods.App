using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files.Diff;

/// <summary>
/// A file diff entry
/// </summary>
public record DiskDiffEntry
{
    /// <summary>
    /// The game path of the file
    /// </summary>
    public required GamePath GamePath { get; init; }
    
    /// <summary>
    /// The size of the file (newest if modified)
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The hash of the file (newest if modified)
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The type of change that occurred to the file path
    /// </summary>
    public required FileChangeType ChangeType { get; init; }
}
