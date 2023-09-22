using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Base class for all games supported by the Nexus app.
/// </summary>
public abstract class AGame : IGame
{
    private IReadOnlyCollection<GameInstallation>? _installations;
    private readonly IEnumerable<IGameLocator> _gamelocators;

    /// <summary/>
    /// <param name="gameLocators">Services used for locating games.</param>
    public AGame(IEnumerable<IGameLocator> gameLocators)
    {
        _gamelocators = gameLocators;
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract GameDomain Domain { get; }

    /// <summary>
    /// The path to the main executable file for the game.
    /// </summary>
    public abstract GamePath GetPrimaryFile(GameStore store);

    /// <summary>
    /// Returns a list of installations for this game.
    /// Each game can have multiple installations, e.g. different game versions.
    /// </summary>
    public virtual IEnumerable<GameInstallation> Installations => _installations ??= GetInstallations();

    /// <inheritdoc />
    public virtual IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    /// <inheritdoc />
    public virtual IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    /// <inheritdoc />
    public virtual IStreamFactory GameImage =>
        throw new NotImplementedException("No game image provided for this game.");

    /// <inheritdoc />
    public virtual IEnumerable<IModInstaller> Installers { get; } = Array.Empty<IModInstaller>();

    /// <inheritdoc />
    public virtual List<IModInstallDestination> InstallDestinations { get; } = new List<IModInstallDestination>();

    private Version GetVersion(GameLocatorResult installation)
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
        return (from locator in _gamelocators
                from installation in locator.Find(this)
                select new GameInstallation
                {
                    Game = this,
                    LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>(
                        GetLocations(installation.Path.FileSystem, installation))),
                    Version = installation.Version ?? GetVersion(installation),
                    Store = installation.Store
                })
            .DistinctBy(g => g.LocationsRegister[LocationId.Game])
            .ToList();
    }

    /// <summary>
    /// Returns the locations of known game elements, such as save folder, etc.
    /// </summary>
    /// <remarks>
    /// TODO: (Al12rs) Games can return Locations that point to the same AbsolutePath, a way is needed to decide which to use.
    /// Current code will use the first declared one but relies on undefined ordering of Dictionary.
    /// </remarks>
    /// <param name="fileSystem">The file system where the game was found in. This comes from <paramref name="installation"/>.</param>
    /// <param name="installation">An installation of the game found by the <paramref name="locator"/>.</param>
    /// <returns></returns>
    protected abstract IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation);

    /// <inheritdoc />
    public override string ToString() => Name;
}
