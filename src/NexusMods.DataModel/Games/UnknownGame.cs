using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public class UnknownGame : IGame
{
    private readonly string _slugName;
    private readonly Version _version;

    public UnknownGame(string name, Version version)
    {
        _slugName = name;
        _version = version;
    }

    public string Name => $"Unknown Game ({_slugName})";
    public string Slug => _slugName;

    public IEnumerable<GameInstallation> Installations => new[]
    {
        new GameInstallation
        {
            Game = this,
            Locations = new Dictionary<GameFolderType, AbsolutePath>(),
            Version = _version
        }
    };
}