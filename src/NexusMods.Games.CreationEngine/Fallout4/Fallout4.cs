using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.Fallout4;

public class Fallout4 : AGame, ISteamGame, IGogGame
{
    public Fallout4(IServiceProvider provider) : base(provider)
    {
    }

    public override string Name => "Fallout 4";
    public override GameId GameId => GameId.From(1151);
    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "Fallout4.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        return [];
    }

    public override SupportType SupportType => SupportType.Unsupported;
    public IEnumerable<uint> SteamIds => [377160];
    public IEnumerable<long> GogIds => [ 1998527297 ];
        
    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<Fallout4>("NexusMods.Games.CreationEngine.Resources.Fallout4.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<Fallout4>("NexusMods.Games.CreationEngine.Resources.Fallout4.tile.webp");
}
