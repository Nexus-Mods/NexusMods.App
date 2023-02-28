using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Interface for a specific game recognized by the app. A single game can have
/// multiple installations.
/// </summary>
public interface IGame
{
    /// <summary>
    /// Human friendly name for the game
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Machine friendly name for the game, should be devoid of special characters
    /// that may conflict with URLs or file paths.
    /// </summary>
    public GameDomain Domain { get; }
    
    /// <summary>
    /// IEnumerable of all valid installations of this game on this machine
    /// </summary>
    public IEnumerable<GameInstallation> Installations { get; }
    
    /// <summary>
    /// Returns any files that should be placed in the "Game Files" that are generated or maintained
    /// by this IGame instance.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store);
    
    /// <summary>
    /// Stream factory for the game's icon, must be square but need not be small.
    /// </summary>
    public IStreamFactory Icon { get; }
}