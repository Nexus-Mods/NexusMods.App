using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

[UsedImplicitly]
public class StardewValley : AGame, ISteamGame, IGogGame, IXboxGame
{
    private readonly IOSInformation _osInformation;
    private readonly IFileSystem _fileSystem;

    public IEnumerable<uint> SteamIds => new[] { 413150u };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };
    public IEnumerable<string> XboxIds => new[] { "ConcernedApe.StardewValleyPC" };

    public override string Name => "Stardew Valley";

    public static GameDomain GameDomain => GameDomain.From("stardewvalley");
    public override GameDomain Domain => GameDomain;

    public StardewValley(
        IOSInformation osInformation,
        IFileSystem fileSystem,
        IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
        _osInformation = osInformation;
        _fileSystem = fileSystem;
    }

    public override GamePath GetPrimaryFile(GameStore store)
    {
        // "StardewValley" is a wrapper shell script that launches the "Stardew Valley" binary
        // on OSX, it's used to check the .NET version, on Linux it just launches the game
        // for XboxGamePass, the original exe gets replaced during installation
        return _osInformation.MatchPlatform(
            state: ref store,
            onWindows: (ref GameStore gameStore) =>
                gameStore == GameStore.XboxGamePass
                    ? new GamePath(GameFolderType.Game, "Stardew Valley.exe")
                    : new GamePath(GameFolderType.Game, "StardewModdingAPI.exe"),
            onLinux: (ref GameStore _) => new GamePath(GameFolderType.Game, "StardewValley"),
            onOSX: (ref GameStore _) => new GamePath(GameFolderType.Game, "Contents/MacOS/StardewValley")
        );
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        if (installation.Store == GameStore.XboxGamePass)
        {
            yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path.Combine("Content"));
        }
        else
        {
            yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
        }

        var stardewValleyAppDataPath = fileSystem
            .GetKnownPath(KnownPath.ApplicationDataDirectory)
            .Combine("StardewValley");

        // base game saves
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(
            GameFolderType.Saves,
            stardewValleyAppDataPath.Combine("Saves")
        );

        // global data files (https://github.com/Pathoschild/SMAPI/blob/8d600e226960a81636137d9bf286c69ab39066ed/src/SMAPI/Framework/ModHelpers/DataHelper.cs#L163-L169)
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(
            GameFolderType.AppData,
            stardewValleyAppDataPath.Combine(".smapi")
        );
    }

    public override IStreamFactory Icon => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.icon.png");

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.game_image.jpg");
}
