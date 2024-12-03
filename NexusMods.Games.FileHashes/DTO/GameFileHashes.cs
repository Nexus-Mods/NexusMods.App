using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.DTO;

/// <summary>
/// A game file and its associated hashes
/// </summary>
public class GameFileHashes
{
    /// <summary>
    /// The path to the game file
    /// </summary>
    public required RelativePath Path { get; init; }
    
    /// <summary>
    /// The xxHash3 hash of the game file
    /// </summary>
    public required Hash XxHash3 { get; init; }
    
    /// <summary>
    /// The minimal xxHash3 hash of the game file
    /// </summary>
    public required Hash MinimalHash { get; init; }
  
}
