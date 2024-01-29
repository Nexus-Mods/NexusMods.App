using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Interface for a specific game recognized by the app. A single game can have
/// multiple installations.
/// </summary>
public interface IGame : ILocatableGame
{
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

    /// <summary>
    /// Returns a <see cref="ILoadoutSynchronizer"/> for this game.
    /// </summary>
    public ILoadoutSynchronizer Synchronizer { get; }
}
