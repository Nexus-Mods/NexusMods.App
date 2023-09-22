using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;

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
    /// <remarks>
    ///    Usually we match these with NexusMods' URLs.
    /// </remarks>
    public GameDomain Domain { get; }

    /// <summary>
    /// IEnumerable of all valid installations of this game on this machine
    /// </summary>
    public IEnumerable<GameInstallation> Installations { get; }

    /// <summary>
    /// Resets the internal cache of installations, forcing a re-scan on the next access of <see cref="Installations"/>.
    /// </summary>
    public void ResetInstallations();

    /// <summary>
    /// Returns any files that should be placed in the "Game Files" that are generated or maintained
    /// by this <see cref="IGame"/> instance.
    /// </summary>
    /// <param name="installation">Individual installation of the game.</param>
    /// <param name="store">Data store [usually database] where information about game files can be cached/stored.</param>
    /// <returns></returns>
    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store);

    /// <summary>
    /// Stream factory for the game's icon, must be square but need not be small.
    /// </summary>
    public IStreamFactory Icon { get; }

    /// <summary>
    /// Stream factory for the game's image, should be close to 16:9 aspect ratio.
    /// </summary>
    public IStreamFactory GameImage { get; }

    /// <summary>
    /// A collection of all <see cref="IModInstaller"/>s that this game supports. The installers
    /// will be tested against a mod's files in the order they are returned by this property.
    /// </summary>
    public IEnumerable<IModInstaller> Installers { get; }
}
