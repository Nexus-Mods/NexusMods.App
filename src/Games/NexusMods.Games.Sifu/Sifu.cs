using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.Sifu;

public class Sifu : AGame, ISteamGame, IEpicGame
{
    private readonly IServiceProvider _serviceProvider;
    public IEnumerable<uint> SteamIds => new[] { 2138710u };
    public IEnumerable<string> EpicCatalogItemId => new[] { "c80a76de890145edbe0d41679dbccc66" };

    public override string Name => "Sifu";

    public override GameDomain Domain => GameDomain.From("sifu");
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return new(GameFolderType.Game, "Sifu.exe");
    }

    public Sifu(IEnumerable<IGameLocator> gameLocators, IServiceProvider serviceProvider) : base(gameLocators)
    {
        _serviceProvider = serviceProvider;
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => new[] { new SifuModInstaller(_serviceProvider) };
}
