using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Abstractions.Games.FileHashes;

/// <summary>
/// A struct that contains information about a file in a game
/// </summary>
public record struct GameFileRecord
{
    /// <summary>
    /// The path to the file
    /// </summary>
    public required GamePath Path { get; init; }
    
    /// <summary>
    /// The size of the game file
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The minimal hash of the game file
    /// </summary>
    public required Hash MinimalHash { get; init; }
    
    /// <summary>
    /// The full hash of the game file
    /// </summary>
    public required Hash Hash { get; init; }
}
