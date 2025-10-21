using Bannerlord.ModuleManager;
using DynamicData.Kernel;
using FetchBannerlordVersion;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;
using NexusMods.Paths;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.NexusModsApi;
using static NexusMods.Games.MountAndBlade2Bannerlord.BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Maintained by the BUTR Team
/// https://github.com/BUTR
/// </summary>
public sealed class Bannerlord : AGame, ISteamGame, IGogGame, IXboxGame//, IEpicGame
{
    public static readonly GameId GameIdStatic = Sdk.NexusModsApi.GameId.From(3174);
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

    // The Epic Games Store is not supported yet, managing the game will put the user into a state where they cannot apply a loadout. 
    // public IEnumerable<string> EpicCatalogItemId => ["Chickadee"];
    public IEnumerable<string> XboxIds => ["TaleWorldsEntertainment.MountBladeIIBannerlord"];

    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.tile.webp");

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        _serviceProvider.GetRequiredService<BLSEInstaller>(),
        _serviceProvider.GetRequiredService<BannerlordModInstaller>(),
    ];
    public override IDiagnosticEmitter[] DiagnosticEmitters => 
    [
        new BannerlordDiagnosticEmitter(_serviceProvider),
        new MissingProtontricksEmitter(_serviceProvider),
    ];

    public Bannerlord(IServiceProvider serviceProvider, LauncherManagerFactory launcherManagerFactory) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => GamePathProvier.PrimaryLauncherFile(targetInfo.Store);

    public override Optional<Version> GetLocalVersion(GameTargetInfo targetInfo, AbsolutePath installationPath)
    {
        // Note(sewer): Bannerlord can use prefixes on versions etc. ,we want to strip them out
        // so we sanitize/parse with `ApplicationVersion`.
        var bannerlordVerStr = Fetcher.GetVersion(installationPath.ToNativeSeparators(OSInformation.Shared), "TaleWorlds.Library.dll");
        var versionStr = ApplicationVersion.TryParse(bannerlordVerStr, out var av) ? $"{av.Major}.{av.Minor}.{av.Revision}.{av.ChangeSet}" : "0.0.0.0";
        return Version.TryParse(versionStr, out var val) ? val : new Version();
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
        return new BannerlordLoadoutSynchronizer(provider);
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
