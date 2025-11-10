using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Base class for all games supported by the Nexus app.
/// </summary>
[PublicAPI]
public abstract class AGame : IGame
{
    private readonly Lazy<ILoadoutSynchronizer> _synchronizer;
    private readonly Lazy<ISortOrderManager> _sortOrderManager;
    private readonly IServiceProvider _provider;
    private readonly IFileSystem _fs;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AGame(IServiceProvider provider)
    {
        _provider = provider;
        // In a Lazy so we don't get a circular dependency
        _synchronizer = new Lazy<ILoadoutSynchronizer>(() => MakeSynchronizer(provider));
        _sortOrderManager = new Lazy<ISortOrderManager>(() => MakeSortOrderManager(provider, this));
        _fs = provider.GetRequiredService<IFileSystem>();
    }

    /// <summary>
    /// Called to create the synchronizer for this game.
    /// </summary>
    protected virtual ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new DefaultSynchronizer(provider);
    }
    
    private ISortOrderManager MakeSortOrderManager(IServiceProvider provider, IGame game)
    {
        var manager = provider.GetRequiredService<SortOrderManager>();
        manager.RegisterSortOrderVarieties(GetSortOrderVarieties(), game);
        return manager;
    }

    GameId IGameData.GameId => GameIdImpl;
    protected abstract GameId GameIdImpl { get; }

    string IGameData.DisplayName => DisplayNameImpl;
    protected abstract string DisplayNameImpl { get; }

    Optional<Sdk.NexusModsApi.NexusModsGameId> IGameData.NexusModsGameId => NexusModsGameIdImpl;
    protected abstract Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameIdImpl { get; }

    /// <inheritdoc/>
    public abstract GamePath GetPrimaryFile(GameTargetInfo targetInfo);

    /// <inheritdoc />
    public abstract IStreamFactory IconImage { get; }

    /// <inheritdoc />
    public abstract IStreamFactory TileImage { get; }

    /// <inheritdoc />
    public virtual ILibraryItemInstaller[] LibraryItemInstallers { get; } = [];

    /// <inheritdoc/>
    public virtual IDiagnosticEmitter[] DiagnosticEmitters { get; } = [];

    /// <inheritdoc />
    public virtual ILoadoutSynchronizer Synchronizer => _synchronizer.Value;
    
    public virtual ISortOrderManager SortOrderManager => _sortOrderManager.Value;

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
            TargetOS = metadata.TargetOS,
            LocatorResultMetadata = metadata.Metadata,
            Locator = locator,
            GameMetadataId = dbId,
        };
    }

    public virtual Optional<Version> GetLocalVersion(GameInstallMetadata.ReadOnly metadata, GameInstallation installation)
    {
        return GetLocalVersion(
            targetInfo: installation.TargetInfo,
            installationPath: _fs.FromUnsanitizedFullPath(metadata.Path)
        );
    }

    /// <summary>
    /// Returns a game specific version of the game, usually from the primary executable.
    /// Usually used for game specific diagnostics.
    /// </summary>
    public virtual Optional<Version> GetLocalVersion(GameTargetInfo targetInfo, AbsolutePath installationPath)
    {
        try
        {
            var primaryFile = GetPrimaryFile(targetInfo);
            var fvi = installationPath.Combine(primaryFile.Path).FileInfo.GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception)
        {
            return Optional<Version>.None;
        }
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

    /// <summary>
    /// Returns the Sort Order Variety definitions supported by this game.
    /// </summary>
    /// <returns></returns>
    protected virtual ISortOrderVariety[] GetSortOrderVarieties() => [];

    /// <inheritdoc/>
    public virtual Optional<GamePath> GetFallbackCollectionInstallDirectory(GameTargetInfo targetInfo) => Optional<GamePath>.None;

    /// <inheritdoc />
    public override string ToString() => DisplayNameImpl;
}
