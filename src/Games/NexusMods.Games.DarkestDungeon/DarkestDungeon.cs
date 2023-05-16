using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeon : AGame, ISteamGame, IGogGame
{
    private readonly IOSInformation _osInformation;

    public IEnumerable<int> SteamIds => new[] { 262060 };
    public IEnumerable<long> GogIds => new long[] { 1450711444 };

    public DarkestDungeon(
        IOSInformation osInformation,
        IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
        _osInformation = osInformation;
    }

    public override string Name => "Darkest Dungeon";
    public override GameDomain Domain => GameDomain.From("darkestdungeon");

    public override GamePath GetPrimaryFile(GameStore store)
    {
        return _osInformation.MatchPlatform(
            onWindows: () => new GamePath(GameFolderType.Game, @"_windowsnosteam\Darkest.exe"),
            onLinux: () => new GamePath(GameFolderType.Game, "_linuxnosteam/darkest.bin.x86_64")
        );
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.game_image.jpg");



}
