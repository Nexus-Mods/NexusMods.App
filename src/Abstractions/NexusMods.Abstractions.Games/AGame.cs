using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
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
    private readonly Lazy<ILoadoutSynchronizer> _synchronizer;
    private readonly IServiceProvider _provider;
    private readonly IFileSystem _fs;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AGame(IServiceProvider provider)
    {
        _provider = provider;
        _gameLocators = provider.GetServices<IGameLocator>();
        // In a Lazy so we don't get a circular dependency
        _synchronizer = new Lazy<ILoadoutSynchronizer>(() => MakeSynchronizer(provider));
        _fs = provider.GetRequiredService<IFileSystem>();
    }

    /// <summary>
    /// Called to create the synchronizer for this game.
    /// </summary>
    protected virtual ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new DefaultSynchronizer(provider);
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract SupportType SupportType { get; }

    /// <inheritdoc />
    public virtual HashSet<FeatureStatus> Features { get; } = [];

    /// <inheritdoc />
    public abstract GameId GameId { get; }

    /// <summary>
    /// The path to the main executable file for the game.
    /// </summary>
    public abstract GamePath GetPrimaryFile(GameStore store);
    
    /// <inheritdoc />
    public virtual IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    /// <inheritdoc />
    public virtual IStreamFactory GameImage => throw new NotImplementedException("No game image provided for this game.");
    
    /// <inheritdoc />
    public virtual ILibraryItemInstaller[] LibraryItemInstallers { get; } = [];

    /// <inheritdoc/>
    public virtual IDiagnosticEmitter[] DiagnosticEmitters { get; } = [];

    /// <inheritdoc/>
    public virtual ISortableItemProviderFactory[] SortableItemProviderFactories { get; } = [];

    /// <inheritdoc />
    public virtual ILoadoutSynchronizer Synchronizer => _synchronizer.Value;

    /// <inheritdoc />
    public GameInstallation InstallationFromLocatorResult(GameLocatorResult metadata, EntityId dbId, IGameLocator locator)
    {
        var locations = GetLocations(metadata.GameFileSystem, metadata);
        return new GameInstallation
        {
            Game = this,
            LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>(locations)),
            InstallDestinations = GetInstallDestinations(locations),
            Store = metadata.Store,
            LocatorResultMetadata = metadata.Metadata,
            Locator = locator,
            GameMetadataId = dbId,
        };
    }

    /// <summary>
    /// Returns a game specific version of the game, usually from the primary executable.
    /// Usually used for game specific diagnostics.
    /// </summary>
    public virtual Version GetLocalVersion(GameInstallMetadata.ReadOnly installation)
    {
        try
        {
            var fvi = GetPrimaryFile(installation.Store)
                .Combine(_fs.FromUnsanitizedFullPath(installation.Path)).FileInfo
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

    /// <inheritdoc/>
    public virtual Optional<GamePath> GetFallbackCollectionInstallDirectory() => Optional<GamePath>.None;

    /// <inheritdoc />
    public override string ToString() => Name;
}
