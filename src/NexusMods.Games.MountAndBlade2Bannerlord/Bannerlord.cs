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
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;
using static NexusMods.Games.MountAndBlade2Bannerlord.BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Maintained by the BUTR Team
/// https://github.com/BUTR
/// </summary>
public sealed class Bannerlord : AGame, ISteamGame, IGogGame, IXboxGame, IGameData<Bannerlord>
{
    private readonly IServiceProvider _serviceProvider;

    public static GameId GameId { get; } = GameId.From("Bannerlord");
    protected override GameId GameIdImpl => GameId;

    public static string DisplayName => "Mount & Blade II: Bannerlord";
    protected override string DisplayNameImpl => DisplayName;

    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(3174);
    protected override Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameIdImpl => NexusModsGameId;

    public override StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [261550u],
        GOGProductIds = [1802539526L, 1564781494L],
        EGSCatalogItemId = ["Chickadee"],
        XboxPackageIdentifiers = ["TaleWorldsEntertainment.MountBladeIIBannerlord"],
    };

    public IEnumerable<uint> SteamIds => StoreIdentifiers.SteamAppIds;
    public IEnumerable<long> GogIds => StoreIdentifiers.GOGProductIds;
    public IEnumerable<string> EpicCatalogItemId => StoreIdentifiers.EGSCatalogItemId;
    public IEnumerable<string> XboxIds => StoreIdentifiers.XboxPackageIdentifiers;

    public override IStreamFactory IconImage => new EmbeddedResourceStreamFactory<Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.thumbnail.webp");
    public override IStreamFactory TileImage => new EmbeddedResourceStreamFactory<Bannerlord>("NexusMods.Games.MountAndBlade2Bannerlord.Resources.tile.webp");

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

    public Bannerlord(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
