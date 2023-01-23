using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimSpecialEdition : AGame, ISteamGame, IGogGame
{
    public static Extension ESL = new(".esl");
    public static Extension ESM = new(".esm");
    public static Extension ESP = new(".esp");
    
    public static HashSet<Extension> PluginExtensions = new() { ESL, ESM, ESP };
    public static string StaticSlug => "skyrimspecialedition";
    public override string Name => "Skyrim Special Edition";
    public override string Slug => StaticSlug;
    public override GamePath PrimaryFile => new(GameFolderType.Game, "SkyrimSE.exe");


    public SkyrimSpecialEdition(ILogger<SkyrimSpecialEdition> logger, IEnumerable<IGameLocator> gameLocators) : base(logger, gameLocators)
    {
    }
    
    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new (GameFolderType.Game, installation.Path);
        var appData = locator switch
        {
            SteamLocator => KnownFolders.MyGames.Join("Skyrim Special Edition"),
            GogLocator => KnownFolders.MyGames.Join("Skyrim Special Edition GOG"),
            _ => throw new NotImplementedException($"No override for {locator}")
        };
        yield return new(GameFolderType.AppData, appData);
    }

    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        yield return new PluginFile
        {
            To = new GamePath(GameFolderType.AppData, "plugins.txt"),
            Size = Size.Zero,
            Hash = Hash.Zero,
            Store = store
        };
    }

    public IEnumerable<int> SteamIds => new[] { 489830 };

    public IEnumerable<long> GogIds => new long[]
    {
        1711230643, // The Elder Scrolls V: Skyrim Special Edition AKA Base Game
        1801825368, // The Elder Scrolls V: Skyrim Anniversary Edition AKA The Store Bundle 
        1162721350 // Upgrade DLC
    };
}