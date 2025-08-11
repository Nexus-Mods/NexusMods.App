using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public class SkyrimSE : AGame, ISteamGame, IGogGame
{
    public SkyrimSE(IServiceProvider provider) : base(provider)
    {
    }

    public override string Name => "Skyrim Special Edition";
    public override GameId GameId => GameId.From(1704);
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "SkyrimSE.exe");

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
    public IEnumerable<uint> SteamIds => [489830];
    public IEnumerable<long> GogIds => [ 1711230643 ];
    
    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.tile.webp");
}
