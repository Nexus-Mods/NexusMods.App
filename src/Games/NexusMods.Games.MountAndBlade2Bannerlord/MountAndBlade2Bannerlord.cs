using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
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
    public static readonly GameId GameIdStatic = GameId.From(3174);
    public static readonly GameDomain DomainStatic = GameDomain.From("mountandblade2bannerlord");
    public static string DisplayName => "Mount & Blade II: Bannerlord";

    private readonly IServiceProvider _serviceProvider;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public override string Name => DisplayName;
    public override GameId GameId => GameIdStatic;
    public override SupportType SupportType => SupportType.Official;

    public override HashSet<FeatureStatus> Features { get; } =
    [
        new(BaseFeatures.GameLocatable, IsImplemented: true),
        new(BaseFeatures.HasInstallers, IsImplemented: true),
        new(BaseFeatures.HasDiagnostics, IsImplemented: false),
        new(BaseFeatures.HasLoadOrder, IsImplemented: false),
    ];

    public IEnumerable<uint> SteamIds => [261550u];
    public IEnumerable<long> GogIds => [1802539526, 1564781494];
    public IEnumerable<string> EpicCatalogItemId => ["Chickadee"];
    public IEnumerable<string> XboxIds => ["TaleWorldsEntertainment.MountBladeIIBannerlord"];

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.icon.jpg");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<MountAndBlade2Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.game_image.jpg");

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        _serviceProvider.GetRequiredService<MountAndBlade2BannerlordModInstaller>(),
    ];
    
    public MountAndBlade2Bannerlord(IServiceProvider serviceProvider, LauncherManagerFactory launcherManagerFactory) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public override GamePath GetPrimaryFile(GameStore store) => GamePathProvier.PrimaryLauncherFile(store);

    protected override Version GetVersion(GameLocatorResult installation)
    {
        var launcherManagerHandler = _launcherManagerFactory.Get(installation);
        return Version.TryParse(launcherManagerHandler.GetGameVersion(), out var val) ? val : new Version();
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var documentsFolder = fileSystem.GetKnownPath(KnownPath.MyDocumentsDirectory);
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, installation.Store == GameStore.XboxGamePass ? installation.Path.Combine("Content") : installation.Path },
            { LocationId.Saves, documentsFolder.Combine($"{DocumentsFolderName}/Game Saves") },
            { LocationId.Preferences, documentsFolder.Combine($"{DocumentsFolderName}/Configs") },
        };
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new MountAndBlade2BannerlordLoadoutSynchronizer(provider);
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
