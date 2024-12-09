using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.FileHashes.HashValues;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.DTO;

/// <summary>
/// A game file and its associated hashes
/// </summary>
public class GameFileHashes
{
    /// <summary>
    /// The (string) identifier of the game
    /// </summary>
    public required GameId GameId { get; init; }
    
    /// <summary>
    /// The (string) identifier of the strore the game came from
    /// </summary>
    public required GameStore Store { get; init; }
    
    /// <summary>
    /// The game version
    /// </summary>
    public required Version Version { get; init; }
    
    /// <summary>
    /// The associated operating system
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public required OSType OS { get; init; } = OSType.Windows;
    
    /// <summary>
    /// The path to the game file
    /// </summary>
    public required RelativePath Path { get; set; } = string.Empty;
    
    /// <summary>
    /// The size of the game file in bytes
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The xxHash3 hash of the game file
    /// </summary>
    public required Hash XxHash3 { get; init; }
    
    /// <summary>
    /// The minimal xxHash3 hash of the game file
    /// </summary>
    public required Hash MinimalHash { get; init; }
    
    /// <summary>
    /// The Sha1 hash of the game file
    /// </summary>
    public required Sha1Hash Sha1 { get; init; }
    
    /// <summary>
    /// The Md5 hash of the game file
    /// </summary>
    public required Md5Hash Md5 { get; init; }
  
}
