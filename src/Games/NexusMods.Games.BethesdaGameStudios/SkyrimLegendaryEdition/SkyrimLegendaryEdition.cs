using NexusMods.Common;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

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
