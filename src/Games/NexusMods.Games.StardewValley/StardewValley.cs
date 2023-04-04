using System.Runtime.InteropServices;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

public class StardewValley : AGame, ISteamGame, IGogGame
{
    public IEnumerable<int> SteamIds => new[] { 413150 };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };

    public override string Name => "Stardew Valley";

    public static GameDomain GameDomain => DataModel.Games.GameDomain.From("stardewvalley");
    public override GameDomain Domain => GameDomain;

    public StardewValley(IEnumerable<IGameLocator> gameLocators) : base(gameLocators) { }

    public override GamePath PrimaryFile
    {
        get
        {
            // "StardewValley" is a wrapper shell script that launches the "Stardew Valley" binary
            // on OSX, it's used to check the .NET version, on Linux it just launches the game
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new GamePath(GameFolderType.Game, "StardewValley");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new GamePath(GameFolderType.Game,"Contents/MacOS/StardewValley");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new GamePath(GameFolderType.Game, "Stardew Valley.exe");
            }

            throw new PlatformNotSupportedException();
        }
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }

    public override IStreamFactory Icon => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.icon.png");

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.game_image.jpg");
}
