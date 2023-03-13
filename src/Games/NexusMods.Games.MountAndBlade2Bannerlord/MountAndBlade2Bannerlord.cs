using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Maintained by the BUTR Team
/// https://github.com/BUTR
/// </summary>
public sealed class MountAndBlade2Bannerlord : AGame, ISteamGame, IGogGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("mountandblade2bannerlord");
    public static string DisplayName => "Mount & Blade II: Bannerlord";

    private readonly IEnumerable<IGameLocator> _gameLocators;
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private IReadOnlyCollection<GameInstallation>? _installations;

    public IEnumerable<int> SteamIds => new[] { 261550 };
    public IEnumerable<long> GogIds => new long[] { 1802539526, 1564781494 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "Chickadee" };
    public IEnumerable<string> XboxIds => new[] { "TaleWorldsEntertainment.MountBladeIIBannerlord" };

    public MountAndBlade2Bannerlord(IEnumerable<IGameLocator> gameLocators, LauncherManagerFactory launcherManagerFactory) : base(gameLocators)
    {
        _gameLocators = gameLocators;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public override string Name => DisplayName;
    public override GameDomain Domain => StaticDomain;
    public override GamePath PrimaryFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.Launcher.exe");
    public GamePath PrimaryXboxFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\Launcher.Native.exe");
    public GamePath PrimaryStandaloneFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\Bannerlord.exe");

    public GamePath BLSEStandaloneFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\Bannerlord.BLSE.Standalone.exe");
    public GamePath BLSELauncherFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\Bannerlord.BLSE.Launcher.exe");

    public GamePath BLSELauncherExFile => new(GameFolderType.Game, @"bin\Win64_Shipping_Client\Bannerlord.BLSE.LauncherEx.exe");

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.MountAndBlade2Bannerlord.icon.jpg");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.MountAndBlade2Bannerlord.game_image.jpg");

    public override IEnumerable<GameInstallation> Installations
    {
        get
        {
            if (_installations != null) return _installations;
            _installations = _gameLocators.SelectMany(locator => locator.Find(this), (locator, installation) =>
                {
                    var launcherManagerHandler = _launcherManagerFactory.Get(installation.Path.ToString());
                    return new GameInstallation
                    {
                        Game = this,
                        Locations = new Dictionary<GameFolderType, AbsolutePath>(GetLocations(locator, installation)),
                        Version = Version.TryParse(launcherManagerHandler.GetGameVersion(), out var val) ? val : new Version(),
                    };
                })
                .DistinctBy(g => g.Locations[GameFolderType.Game])
                .ToList();
            return _installations;
        }
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves, KnownFolders.Documents.CombineChecked(@"Mount and Blade II Bannerlord\Game Saves"));
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Preferences, KnownFolders.Documents.CombineChecked(@"Mount and Blade II Bannerlord\Configs"));
    }
}
