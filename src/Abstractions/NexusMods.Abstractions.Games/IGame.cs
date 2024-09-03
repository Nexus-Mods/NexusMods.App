using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Interface for a specific game recognized by the app. A single game can have
/// multiple installations.
/// </summary>
public interface IGame : IGameWithSynchronizer
{
    /// <summary>
    /// Stream factory for the game's icon, must be square but need not be small.
    /// </summary>
    public IStreamFactory Icon { get; }

    /// <summary>
    /// Stream factory for the game's image, should be close to 16:9 aspect ratio.
    /// </summary>
    public IStreamFactory GameImage { get; }
    
    /// <summary>
    /// Gets all available installers this game supports.
    /// </summary>
    public ILibraryItemInstaller[] LibraryItemInstallers { get; }

    /// <summary>
    /// An array of all instances of <see cref="IDiagnosticEmitter"/> supported
    /// by the game.
    /// </summary>
    public IDiagnosticEmitter[] DiagnosticEmitters { get; }
    
    /// <summary>
    /// Constructs a <see cref="GameInstallation"/> from the given <see cref="GameLocatorResult"/>, and a unique DB ID,
    /// also marks the installation was sourced from the given <see cref="IGameLocator"/>.
    /// </summary>
    public GameInstallation InstallationFromLocatorResult(GameLocatorResult metadata, EntityId dbId, IGameLocator locator);
}
