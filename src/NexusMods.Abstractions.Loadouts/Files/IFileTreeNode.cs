using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files;


/// <summary>
/// Interface for things that want to be put into a file tree
/// </summary>
public interface IFileTreeNode
{
    /// <summary>
    /// Path of the file
    /// </summary>
    GamePath To { get; }
    
    /// <summary>
    /// Hash of the file
    /// </summary>
    Hash Hash { get; }
    
    /// <summary>
    /// Size of the file
    /// </summary>
    Size Size { get; }
}
