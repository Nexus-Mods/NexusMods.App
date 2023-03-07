using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public abstract class AGame : IGame
{
    private IReadOnlyCollection<GameInstallation>? _installations;
    private readonly IEnumerable<IGameLocator> _gamelocators;
    protected readonly ILogger _logger;

    public AGame(ILogger logger, IEnumerable<IGameLocator> gameLocators)
    {
        _logger = logger;
        _gamelocators = gameLocators;
    }
    public abstract string Name { get; }
    public abstract GameDomain Domain { get; }
    public abstract GamePath PrimaryFile { get; }

    public virtual IEnumerable<GameInstallation> Installations
    {
        get
        {
            if (_installations != null) return _installations;
            _installations = (from locator in _gamelocators
                              from installation in locator.Find(this)
                              select new GameInstallation
                              {
                                  Game = this,
                                  Locations = new Dictionary<GameFolderType, AbsolutePath>(GetLocations(locator, installation)),
                                  Version = installation.Version ?? GetVersion(locator, installation)
                              })
                .DistinctBy(g => g.Locations[GameFolderType.Game])
                .ToList();
            return _installations;
        }
    }

    public virtual IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    public virtual IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    private Version GetVersion(IGameLocator locator, GameLocatorResult installation)
    {
        var fvi = PrimaryFile.CombineChecked(installation.Path).VersionInfo;
        return fvi.ProductVersion == null ? new Version("1.0.0.0") : Version.Parse(fvi.ProductVersion!);
    }

    public override string ToString()
    {
        return Name;
    }

    protected abstract IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation);
}
