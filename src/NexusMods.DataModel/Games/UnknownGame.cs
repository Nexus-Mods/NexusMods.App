using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public class UnknownGame : IGame
{
    private readonly GameDomain _slugName;
    private readonly Version _version;

    public UnknownGame(GameDomain domain, Version version)
    {
        _slugName = domain;
        _version = version;
    }

    public string Name => $"Unknown Game ({_slugName})";
    public GameDomain Domain => _slugName;

    public IEnumerable<GameInstallation> Installations => new[]
    {
        new GameInstallation
        {
            Game = this,
            Locations = new Dictionary<GameFolderType, AbsolutePath>(),
            Version = _version
        }
    };

    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    public IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    public IStreamFactory GameImage => throw new NotImplementedException("No game image provided for this game.");
}
