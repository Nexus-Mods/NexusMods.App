using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.Interfaces;

/// <summary>
/// Descriptor object for a single game
/// </summary>
public class GameInstallation
{
    /// <summary>
    /// The Version installed
    /// </summary>
    public Version Version { get; init; } = new();
    
    /// <summary>
    /// The location on-disk of this game and it's associated paths
    /// </summary>
    public IReadOnlyDictionary<GameFolderType, AbsolutePath> Locations { get; init; }
    
    /// <summary>
    /// The game to which this installation belongs
    /// </summary>
    public IGame Game { get; init; } = null!;

    
    public override string ToString()
    {
        return $"{Game.Name} v{Version}";
    }
}