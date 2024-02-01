using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimLegendaryEdition;

public class SkyrimLegendaryEdition : ABethesdaGame, ISteamGame
{
    public SkyrimLegendaryEdition(IServiceProvider provider) : base(provider) { }
    public override string Name => "Skyrim Legendary Edition";

    public static GameDomain StaticDomain => GameDomain.From("skyrim");

    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "TESV.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, installation.Path },
            { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Skyrim") }
        };
    }

    public IEnumerable<uint> SteamIds => new[] { 72850u };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<SkyrimLegendaryEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimLegendaryEdition.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<SkyrimLegendaryEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimLegendaryEdition.game_image.jpg");
}
