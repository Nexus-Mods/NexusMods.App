using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Larian.BaldursGate3.Emitters;
using NexusMods.Games.Larian.BaldursGate3.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3 : AGame, ISteamGame, IGogGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOSInformation _osInformation;
    private readonly IFileSystem _fs;
    public override string Name => "Baldur's Gate 3";

    public IEnumerable<uint> SteamIds => [1086940u];
    public IEnumerable<long> GogIds => [1456460669];
    public override GameId GameId => GameId.From(3474);
    public static readonly GameDomain StaticDomain = GameDomain.From("baldursgate3");
    public override SupportType SupportType => SupportType.Official;

    public override HashSet<FeatureStatus> Features { get; } =
    [
        new(BaseFeatures.GameLocatable, IsImplemented: true),
        new(BaseFeatures.HasInstallers, IsImplemented: true),
        new(BaseFeatures.HasDiagnostics, IsImplemented: true),
        new(BaseFeatures.HasLoadOrder, IsImplemented: false),
    ];

    public BaldursGate3(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _osInformation = provider.GetRequiredService<IOSInformation>();
        _fs = provider.GetRequiredService<IFileSystem>();
    }
    
    public override Optional<Version> GetLocalVersion(GameInstallMetadata.ReadOnly installation)
    {
        try
        {
            // Use the vulkan executable to get the version, not the primary file (launcher)
            var executableGamePath = _osInformation.IsOSX 
                ? new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3") 
                : new GamePath(LocationId.Game, "bin/bg3.exe");

            var fvi = executableGamePath
                .Combine(_fs.FromUnsanitizedFullPath(installation.Path)).FileInfo
                .GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception)
        {
            return Optional<Version>.None;
        }
    }

    public override GamePath GetPrimaryFile(GameStore store)
    {
        if (_osInformation.IsOSX)
            return new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3");
        
        // Use launcher to allow choosing between DirectX11 and Vulkan on GOG, Steam already always starts the launcher
        return new GamePath(LocationId.Game, "Launcher/LariLauncher.exe");
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { Bg3Constants.ModsLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/Mods") },
            { LocationId.From("PlayerProfiles"), fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/PlayerProfiles/Public") },
            { LocationId.From("ScriptExtenderConfig"), fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/ScriptExtender") },
        };
        return result;
    }

    /// <inheritdoc />
    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        return
        [
        ];
    }

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new BG3SEInstaller(_serviceProvider),
        new GenericPatternMatchInstaller(_serviceProvider)
        {
            InstallFolderTargets =
            [
                // Pak mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/366?tab=description">ImpUI (ImprovedUI) Patch7Ready</see>
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/11373?tab=description">NPC Visual Overhaul (WIP) - NPC VO</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(Bg3Constants.ModsLocationId, ""),
                    KnownValidFileExtensions = [Bg3Constants.PakFileExtension],
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt, KnownExtensions.Md, KnownExtensions.Pdf, KnownExtensions.Png,
                        KnownExtensions.Json, new Extension(".lnk"),
                    ],
                },

                // bin and NativeMods mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/944">Native Mod Loader</see>
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/668?tab=files">Achievement Enabler</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, "bin"),
                    KnownSourceFolderNames = ["bin"],
                    Names = ["NativeMods"],
                },

                // loose files Data mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/555?tab=description">Fast XP</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, "Data"),
                    KnownSourceFolderNames = ["Data"],
                    Names = ["Generated", "Public"],
                },
            ],
        },
    ];

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new BaldursGate3Synchronizer(provider);
    }

    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        _serviceProvider.GetRequiredService<DependencyDiagnosticEmitter>(),
    ];

    // TODO: We are using Icon for both Spine and GameWidget and GameImage is unused. We should use GameImage for the GameWidget, but need to update all the games to have better images.
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.icon.png");
}
