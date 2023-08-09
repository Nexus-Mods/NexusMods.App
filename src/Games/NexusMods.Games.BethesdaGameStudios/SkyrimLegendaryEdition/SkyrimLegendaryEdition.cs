using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities;
using NexusMods.DataModel.Games.GameCapabilities.AFolderMatchInstallerCapability;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Games.BethesdaGameStudios.Capabilities;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimLegendaryEdition : ABethesdaGame, ISteamGame
{
    public SkyrimLegendaryEdition(IEnumerable<IGameLocator> gameLocators) : base(gameLocators) { }
    public override string Name => "Skyrim Legendary Edition";

    public static GameDomain StaticDomain => GameDomain.From("skyrim");

    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(GameFolderType.Game, "TESV.exe");

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);

        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.AppData, fileSystem
            .GetKnownPath(KnownPath.LocalApplicationDataDirectory)
            .Combine("Skyrim"));
    }

    public IEnumerable<uint> SteamIds => new[] { 72850u };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<SkyrimLegendaryEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimLegendaryEdition.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<SkyrimLegendaryEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimLegendaryEdition.game_image.jpg");
}
