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
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.FileHashes.Emitters;
using NexusMods.Games.FOMOD;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.StardewValley;

[UsedImplicitly]
public class StardewValley : AGame, ISteamGame, IGogGame, IXboxGame, IGameData<StardewValley>
{
    public static GameDomain DomainStatic => GameDomain.From("stardewvalley");
    private readonly IServiceProvider _serviceProvider;
    public IEnumerable<uint> SteamIds => new[] { 413150u };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };
    public IEnumerable<string> XboxIds => new[] { "ConcernedApe.StardewValleyPC" };

    public static GameId GameId { get; } = GameId.From("StardewValley");
    protected override GameId GameIdImpl => GameId;

    public static string DisplayName => "Stardew Valley";
    protected override string DisplayNameImpl => DisplayName;

    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(1303);
    protected override Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameIdImpl => NexusModsGameId;

    public StardewValley(
        IOSInformation osInformation,
        IEnumerable<IGameLocator> gameLocators,
        IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
    }

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo)
    {
        // NOTE(erri120): Our SMAPI installer overrides all of these files.
        return targetInfo.OS.MatchPlatform(
            onWindows: () => new GamePath(LocationId.Game, "Stardew Valley.exe"),
            onLinux: () => new GamePath(LocationId.Game, "StardewValley"),
            onOSX: () => new GamePath(LocationId.Game, "Contents/MacOS/StardewValley")
        );
    }

    public override Optional<GamePath> GetFallbackCollectionInstallDirectory(GameTargetInfo targetInfo)
    {
        // NOTE(erri120): see https://github.com/Nexus-Mods/NexusMods.App/issues/2553
        var path = targetInfo.OS.MatchPlatform(
            onWindows: () => new GamePath(LocationId.Game, Constants.ModsFolder),
            onLinux: () => new GamePath(LocationId.Game, Constants.ModsFolder),
            onOSX: () => new GamePath(LocationId.Game, "Contents/MacOS" / Constants.ModsFolder)
        );

        return Optional<GamePath>.Create(path);
    }

    public override Optional<Version> GetLocalVersion(GameTargetInfo targetInfo, AbsolutePath installationPath)
    {
        try
        {
            var path = targetInfo.OS.MatchPlatform(
                onWindows: () => "Stardew Valley.dll",
                onLinux: () => "Stardew Valley.dll",
                onOSX: () => "Contents/MacOS/Stardew Valley.dll"
            );

            var fileInfo = installationPath.Combine(path).FileInfo;
            return fileInfo.GetFileVersionInfo().FileVersion;
        }
        catch (Exception)
        {
            return Optional<Version>.None;
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

    public override IStreamFactory IconImage => new EmbeddedResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.thumbnail.webp");
    public override IStreamFactory TileImage => new EmbeddedResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.tile.webp");

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, Constants.ModsFolder)),
        _serviceProvider.GetRequiredService<SMAPIInstaller>(),
        _serviceProvider.GetRequiredService<GenericInstaller>(),
    ];

    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        new NoWayToSourceFilesOnDisk(),
        new UndeployableLoadoutDueToMissingGameFiles(_serviceProvider),
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
