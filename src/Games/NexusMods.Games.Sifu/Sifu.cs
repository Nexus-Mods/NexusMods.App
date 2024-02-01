using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Paths;

namespace NexusMods.Games.Sifu;

public class Sifu : AGame, ISteamGame, IEpicGame
{
    public override string Name => "Sifu";
    public override GameDomain Domain => GameDomain.From("sifu"); // nexus URL
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "Sifu.exe");

    public IEnumerable<uint> SteamIds => new[] { 2138710u };
    public IEnumerable<string> EpicCatalogItemId => new[] { "c80a76de890145edbe0d41679dbccc66" };

    public Sifu(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
    }

    protected override IEnumerable<IModInstaller> MakeInstallers(IServiceProvider provider) => new[] { new SifuModInstaller(provider) };

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
