using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Base class for all games supported by the Nexus app.
/// </summary>
[PublicAPI]
public abstract class AGame : IGame
{
    private IReadOnlyCollection<GameInstallation>? _installations;
    private readonly IEnumerable<IGameLocator> _gameLocators;
    private readonly Lazy<IStandardizedLoadoutSynchronizer> _synchronizer;
    private readonly Lazy<IEnumerable<IModInstaller>> _installers;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AGame(IServiceProvider provider)
    {
        _provider = provider;
        _gameLocators = provider.GetServices<IGameLocator>();
        // In a Lazy so we don't get a circular dependency
        _synchronizer = new Lazy<IStandardizedLoadoutSynchronizer>(() => MakeSynchronizer(provider));
        _installers = new Lazy<IEnumerable<IModInstaller>>(() => MakeInstallers(provider));
    }

    /// <summary>
    /// Helper method to create a <see cref="IStandardizedLoadoutSynchronizer"/>. The result of this method is cached
    /// so that the same instance is returned every time.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    protected virtual IStandardizedLoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new DefaultSynchronizer(provider);
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract GameDomain Domain { get; }

    /// <summary>
    /// The path to the main executable file for the game.
    /// </summary>
    public abstract GamePath GetPrimaryFile(GameStore store);
    
    /// <inheritdoc />
    public virtual IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    /// <inheritdoc />
    public virtual IStreamFactory GameImage => throw new NotImplementedException("No game image provided for this game.");

    /// <inheritdoc />
    public virtual IEnumerable<IModInstaller> Installers => _installers.Value;

    /// <inheritdoc/>
    public virtual IDiagnosticEmitter[] DiagnosticEmitters { get; } = Array.Empty<IDiagnosticEmitter>();

    /// <summary>
    /// Helper method to create a list of <see cref="IModInstaller"/>s. The result of this method is cached
    /// behind a lazy.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    protected virtual IEnumerable<IModInstaller> MakeInstallers(IServiceProvider provider)
    {
        return Array.Empty<IModInstaller>();
    }

    /// <inheritdoc />
    public virtual ILoadoutSynchronizer Synchronizer => _synchronizer.Value;

    /// <inheritdoc />
    public GameInstallation InstallationFromLocatorResult(GameLocatorResult metadata, EntityId dbId, IGameLocator locator)
    {
        var locations = GetLocations(metadata.Path.FileSystem, metadata);
        return new GameInstallation
        {
            Game = this,
            LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>(locations)),
            InstallDestinations = GetInstallDestinations(locations),
            Version = metadata.Version ?? GetVersion(metadata),
            Store = metadata.Store,
            LocatorResultMetadata = metadata.Metadata,
            Locator = locator,
            GameMetadataId = dbId,
        };
    }

    /// <summary>
    /// Returns the game version if GameLocatorResult failed to get the game version.
    /// </summary>
    protected virtual Version GetVersion(GameLocatorResult installation)
    {
        try
        {
            var fvi = GetPrimaryFile(installation.Store)
                .Combine(installation.Path).FileInfo
                .GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception)
        {
            return new Version(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Clears the internal cache of game installations, so that the next access will re-query the system.
    /// </summary>
    public void ResetInstallations()
    {
        _installations = null;
    }

    private List<GameInstallation> GetInstallations()
    {
        return _gameLocators
            .SelectMany(locator => locator.Find(this), (locator, installation) =>
            {
                var locations = GetLocations(installation.Path.FileSystem, installation);
                return new GameInstallation
                {
                    Game = this,
                    LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>(locations)),
                    InstallDestinations = GetInstallDestinations(locations),
                    Version = installation.Version ?? GetVersion(installation),
                    Store = installation.Store,
                    LocatorResultMetadata = installation.Metadata,
                    Locator = locator,
                };
            })
            .DistinctBy(g => g.LocationsRegister[LocationId.Game])
            .ToList();
    }

    /// <summary>
    /// Returns the locations of known game elements, such as save folder, etc.
    /// </summary>
    protected abstract IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation);

    /// <summary>
    /// Returns the locations of installation destinations used by the Advanced Installer.
    /// </summary>
    /// <param name="locations">Result of <see cref="GetLocations"/>.</param>
    public abstract List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations);

    /// <inheritdoc />
    public override string ToString() => Name;
}
