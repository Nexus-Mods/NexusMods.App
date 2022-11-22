using Microsoft.Extensions.Logging;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.Games.Abstractions;

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
    public abstract string Slug { get; }
    public abstract GamePath PrimaryFile { get; }
    
    public IEnumerable<GameInstallation> Installations 
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

    private Version GetVersion(IGameLocator locator, GameLocatorResult installation)
    {
        var fvi = PrimaryFile.RelativeTo(installation.Path).VersionInfo;
        return Version.Parse(fvi.ProductVersion!);
    }

    public override string ToString()
    {
        return Name;
    }

    protected abstract IEnumerable<KeyValuePair<GameFolderType,AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation);
}