using System.Runtime.InteropServices;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeon : AGame, ISteamGame, IGogGame
{
    public IEnumerable<int> SteamIds => new[] { 262060 };
    public IEnumerable<long> GogIds => new long[] { 1450711444 };

    public DarkestDungeon(IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
    }

    public override string Name => "Darkest Dungeon";
    public override GameDomain Domain => GameDomain.From("darkestdungeon");

    public override GamePath GetPrimaryFile(GameStore store)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new GamePath(GameFolderType.Game, @"_linuxnosteam\darkest.bin.x86_64")
            : new GamePath(GameFolderType.Game, @"_windowsnosteam\Darkest.exe");
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.game_image.jpg");



}
