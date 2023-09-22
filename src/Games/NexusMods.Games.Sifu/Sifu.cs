using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
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
        return new(LocationId.Game, "Sifu.exe");
    }

    public Sifu(IEnumerable<IGameLocator> gameLocators, IServiceProvider serviceProvider) : base(gameLocators)
    {
        _serviceProvider = serviceProvider;
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
    }

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => new[] { new SifuModInstaller(_serviceProvider) };

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        var result = new List<IModInstallDestination>();
        ModInstallDestinationHelpers.AddCommonLocations(locations, result);
        return result;
    }
}
