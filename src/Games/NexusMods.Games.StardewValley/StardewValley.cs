using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.FileHashes.Emitters;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

[UsedImplicitly]
public class StardewValley : AGame, ISteamGame, IGogGame, IXboxGame
{
    public static GameDomain DomainStatic => GameDomain.From("stardewvalley");
    private readonly IOSInformation _osInformation;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fs;
    public IEnumerable<uint> SteamIds => new[] { 413150u };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };
    public IEnumerable<string> XboxIds => new[] { "ConcernedApe.StardewValleyPC" };

    public override string Name => "Stardew Valley";
    public override GameId GameId => GameId.From(1303);

    public override SupportType SupportType => SupportType.Official;

    public override HashSet<FeatureStatus> Features { get; } =
    [
        new(BaseFeatures.GameLocatable, IsImplemented: true),
        new(BaseFeatures.HasInstallers, IsImplemented: true),
        new(BaseFeatures.HasDiagnostics, IsImplemented: true),
    ];

    public StardewValley(
        IOSInformation osInformation,
        IEnumerable<IGameLocator> gameLocators,
        IServiceProvider provider) : base(provider)
    {
        _osInformation = osInformation;
        _serviceProvider = provider;
        _fs = provider.GetRequiredService<IFileSystem>();
    }

    public override GamePath GetPrimaryFile(GameStore store)
    {
        // NOTE(erri120): Our SMAPI installer overrides all of these files.
        return _osInformation.MatchPlatform(
            onWindows: () => new GamePath(LocationId.Game, "Stardew Valley.exe"),
            onLinux: () => new GamePath(LocationId.Game, "StardewValley"),
            onOSX: () => new GamePath(LocationId.Game, "Contents/MacOS/StardewValley")
        );
    }

    public override Optional<GamePath> GetFallbackCollectionInstallDirectory()
    {
        // NOTE(erri120): see https://github.com/Nexus-Mods/NexusMods.App/issues/2553
        var path = _osInformation.MatchPlatform(
            onWindows: () => new GamePath(LocationId.Game, "Mods"),
            onLinux: () => new GamePath(LocationId.Game, "Mods"),
            onOSX: () => new GamePath(LocationId.Game, "Contents/MacOS/Mods")
        );

        return Optional<GamePath>.Create(path);
    }

    public override Version GetLocalVersion(GameInstallMetadata.ReadOnly installation)
    {
        try
        {
            var path = _osInformation.MatchPlatform(
                onWindows: () => "Stardew Valley.dll",
                onLinux: () => "Stardew Valley.dll",
                onOSX: () => "Contents/MacOS/Stardew Valley.dll"
            );

            var fileInfo = _fs.FromUnsanitizedFullPath(installation.Path).Combine(path).FileInfo;
            return fileInfo.GetFileVersionInfo().FileVersion;
        }
        catch (Exception)
        {
            return new Version(0, 0, 0, 0);
        }
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
        return result;
    }

    public override IStreamFactory Icon => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.icon.png");

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.game_image.jpg");

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        _serviceProvider.GetRequiredService<SMAPIInstaller>(),
        _serviceProvider.GetRequiredService<SMAPIModInstaller>(),
    ];

    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        new NoWayToSourceFilesOnDisk(),
        new UndeployableLoadoutDueToMissingGameFiles(),
        _serviceProvider.GetRequiredService<SMAPIGameVersionDiagnosticEmitter>(),
        _serviceProvider.GetRequiredService<DependencyDiagnosticEmitter>(),
        _serviceProvider.GetRequiredService<MissingSMAPIEmitter>(),
        _serviceProvider.GetRequiredService<SMAPIModDatabaseCompatibilityDiagnosticEmitter>(),
        _serviceProvider.GetRequiredService<VersionDiagnosticEmitter>(),
        _serviceProvider.GetRequiredService<ModOverwritesGameFilesEmitter>(),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) => ModInstallDestinationHelpers.GetCommonLocations(locations);
    
    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new StardewValleyLoadoutSynchronizer(provider);
    }
}
