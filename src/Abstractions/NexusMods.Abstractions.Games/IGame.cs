using DynamicData.Kernel;
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
public interface IGame : ILocatableGame
{
    SupportType SupportType { get; }
    HashSet<FeatureStatus> Features { get; }
    GameFeatureStatus FeatureStatus => Features.ToStatus();

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
    /// An array of all instances of <see cref="ISortableItemProviderFactory"/> supported
    /// by the game.
    /// </summary>
    public ISortableItemProviderFactory[] SortableItemProviderFactories { get; }

    /// <summary>
    /// The synchronizer for this game.
    /// </summary>
    public ILoadoutSynchronizer Synchronizer { get; }
    
    /// <summary>
    /// Constructs a <see cref="GameInstallation"/> from the given <see cref="GameLocatorResult"/>, and a unique DB ID,
    /// also marks the installation was sourced from the given <see cref="IGameLocator"/>.
    /// </summary>
    public GameInstallation InstallationFromLocatorResult(GameLocatorResult metadata, EntityId dbId, IGameLocator locator);
    
    /// <summary>
    /// Returns the primary (executable) file for the game.
    /// </summary>
    /// <param name="store">The store used for the game.</param>
    public GamePath GetPrimaryFile(GameStore store);

    /// <summary>
    /// Gets the fallback directory for mods in collections that don't have matching installers.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/2553 for details.
    ///
    /// Also is used for bundled mods.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/2630 for details.
    /// </summary>
    Optional<GamePath> GetFallbackCollectionInstallDirectory();
}
