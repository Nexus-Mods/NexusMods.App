using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.GameCapabilities;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Games.Stores.EGS;
using NexusMods.Abstractions.Games.Stores.GOG;
using NexusMods.Abstractions.Games.Stores.Steam;
using NexusMods.Abstractions.Games.Stores.Xbox;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;
using NexusMods.Paths;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Maintained by the BUTR Team
/// https://github.com/BUTR
/// </summary>
public sealed class MountAndBlade2Bannerlord : AGame, ISteamGame, IGogGame, IEpicGame, IXboxGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("mountandblade2bannerlord");
    public static string DisplayName => "Mount & Blade II: Bannerlord";

    private readonly IServiceProvider _serviceProvider;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public IEnumerable<uint> SteamIds => new[] { 261550u };
    public IEnumerable<long> GogIds => new long[] { 1802539526, 1564781494 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "Chickadee" };
    public IEnumerable<string> XboxIds => new[] { "TaleWorldsEntertainment.MountBladeIIBannerlord" };

    public MountAndBlade2Bannerlord(IServiceProvider serviceProvider, LauncherManagerFactory launcherManagerFactory) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public override string Name => DisplayName;
    public override GameDomain Domain => StaticDomain;

    public override GamePath GetPrimaryFile(GameStore store) => GamePathProvier.PrimaryLauncherFile(store);

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.icon.jpg");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.game_image.jpg");

    public override IEnumerable<IModInstaller> Installers => new IModInstaller[]
    {
        MountAndBlade2BannerlordModInstaller.Create(_serviceProvider),
    };

    protected override Version GetVersion(GameLocatorResult installation)
    {
        var launcherManagerHandler = _launcherManagerFactory.Get(installation);
        return Version.TryParse(launcherManagerHandler.GetGameVersion(), out var val) ? val : new Version();
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var documentsFolder = fileSystem.GetKnownPath(KnownPath.MyDocumentsDirectory);
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Store == GameStore.XboxGamePass ? installation.Path.Combine("Content") : installation.Path },
            { LocationId.Saves, documentsFolder.Combine($"{DocumentsFolderName}/Game Saves") },
            { LocationId.Preferences, documentsFolder.Combine($"{DocumentsFolderName}/Configs") },
        };
    }

    protected override IStandardizedLoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new MountAndBlade2BannerlordLoadoutSynchronizer(provider);
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
