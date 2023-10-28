using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.Sifu;

public class Sifu : AGame, ISteamGame, IEpicGame
{
    public IEnumerable<uint> SteamIds => new[] { 2138710u };
    public IEnumerable<string> EpicCatalogItemId => new[] { "c80a76de890145edbe0d41679dbccc66" };

    public override string Name => "Sifu";

    public override GameDomain Domain => GameDomain.From("sifu");
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return new(LocationId.Game, "Sifu.exe");
    }

    public Sifu(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
    }

    protected override IEnumerable<IModInstaller> MakeInstallers(IServiceProvider provider)
    {
        return new[]
        {
            new SifuModInstaller(provider)
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
