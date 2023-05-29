using System.Diagnostics.CodeAnalysis;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeon : AGame, ISteamGame, IGogGame, IEpicGame
{
    private readonly IOSInformation _osInformation;

    public IEnumerable<int> SteamIds => new[] { 262060 };
    public IEnumerable<long> GogIds => new long[] { 1450711444 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "b4eecf70e3fe4e928b78df7855a3fc2d" };

    // TODO: Xbox ID

    public DarkestDungeon(
        IOSInformation osInformation,
        IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
        _osInformation = osInformation;
    }

    public override string Name => "Darkest Dungeon";
    public override GameDomain Domain => GameDomain.From("darkestdungeon");

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return _osInformation.MatchPlatform(
            ref store,
            onWindows: (ref GameStore gameStore) => gameStore == GameStore.Steam
                ? new GamePath(GameFolderType.Game, @"_windows\Darkest.exe")
                : new GamePath(GameFolderType.Game, @"_windowsnosteam\Darkest.exe"),
            onLinux: (ref GameStore gameStore) => gameStore == GameStore.Steam
                ? new GamePath(GameFolderType.Game, "_linux/darkest.bin.x86_64")
                : new GamePath(GameFolderType.Game, "linuxnosteam/darkest.bin.x86_64"),
            onOSX: (ref GameStore gameStore) => gameStore == GameStore.Steam
                ? new GamePath(GameFolderType.Game, "_osx/Darkest.app/Contents/MacOS/Darkest")
                : new GamePath(GameFolderType.Game, "_osxnosteam/Darkest.app/Contents/MacOS/Darkest NoSteam")
        );
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);


        if (installation.Metadata is SteamLocatorResultMetadata { CloudSavesDirectory: not null } steamLocatorResultMetadata)
            yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves, steamLocatorResultMetadata.CloudSavesDirectory.Value);

        var globalSettingsFile = fileSystem
            .GetKnownPath(KnownPath.LocalApplicationDataDirectory)
            .CombineUnchecked("Red Hook Studios")
            .CombineUnchecked("Darkest")
            .CombineUnchecked("persist.options.json");

        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Preferences, globalSettingsFile);

        if (installation.Store == GameStore.Steam)
        {
            // TODO: Steam Cloud Saves
        }
        else
        {
            var savesDirectory = fileSystem
                .GetKnownPath(KnownPath.MyDocumentsDirectory)
                .CombineUnchecked("Darkest");

            yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves, savesDirectory);
        }
    }

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<DarkestDungeon>("NexusMods.Games.DarkestDungeon.Resources.DarkestDungeon.game_image.jpg");



}
