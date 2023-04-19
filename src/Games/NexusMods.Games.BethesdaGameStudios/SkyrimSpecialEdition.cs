using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimSpecialEdition : AGame, ISteamGame, IGogGame
{
    private readonly IFileSystem _fileSystem;

    // ReSharper disable InconsistentNaming
    public static Extension ESL = new(".esl");
    public static Extension ESM = new(".esm");
    public static Extension ESP = new(".esp");
    // ReSharper restore InconsistentNaming

    public static HashSet<Extension> PluginExtensions = new() { ESL, ESM, ESP };
    public static GameDomain StaticDomain => GameDomain.From("skyrimspecialedition");
    public override string Name => "Skyrim Special Edition";
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(GameFolderType.Game, "SkyrimSE.exe");

    public SkyrimSpecialEdition(IEnumerable<IGameLocator> gameLocators, IFileSystem fileSystem) : base(gameLocators)
    {
        _fileSystem = fileSystem;
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new(GameFolderType.Game, installation.Path);
        var appData = locator switch
        {
            GogLocator => _fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).CombineUnchecked("Skyrim Special Edition GOG"),
            _ => _fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).CombineUnchecked("Skyrim Special Edition"),
        };
        yield return new(GameFolderType.AppData, appData);
    }

    public override IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        yield return new PluginFile
        {
            Id = ModFileId.New(),
            To = new GamePath(GameFolderType.AppData, "plugins.txt"),
            Size = Size.Zero,
            Hash = Hash.Zero
        };
    }

    public IEnumerable<int> SteamIds => new[] { 489830 };

    public IEnumerable<long> GogIds => new long[]
    {
        1711230643, // The Elder Scrolls V: Skyrim Special Edition AKA Base Game
        1801825368, // The Elder Scrolls V: Skyrim Anniversary Edition AKA The Store Bundle
        1162721350 // Upgrade DLC
    };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<SkyrimSpecialEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimSpecialEdition.icon.jpg");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<SkyrimSpecialEdition>("NexusMods.Games.BethesdaGameStudios.Resources.SkyrimSpecialEdition.game_image.png");
}
