using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Larian.BaldursGate3.Emitters;
using NexusMods.Games.Larian.BaldursGate3.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3 : AGame, ISteamGame, IGogGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fs;
    public override string DisplayName => "Baldur's Gate 3";

    public IEnumerable<uint> SteamIds => [1086940u];
    public IEnumerable<long> GogIds => [1456460669];
    public override GameId GameId => GameId.From(3474);
    public static readonly GameDomain StaticDomain = GameDomain.From("baldursgate3");

    public BaldursGate3(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _fs = provider.GetRequiredService<IFileSystem>();
    }

    public override Optional<Version> GetLocalVersion(GameTargetInfo targetInfo, AbsolutePath installationPath)
    {
        try
        {
            // Use the vulkan executable to get the version, not the primary file (launcher)
            var executableGamePath = targetInfo.OS.IsOSX 
                ? new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3") 
                : new GamePath(LocationId.Game, "bin/bg3.exe");

            var fvi = executableGamePath
                .Combine(installationPath).FileInfo
                .GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception)
        {
            return Optional<Version>.None;
        }
    }

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo)
    {
        if (targetInfo.OS.IsOSX) return new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3");

        // Use launcher to allow choosing between DirectX11 and Vulkan on GOG, Steam already always starts the launcher
        return new GamePath(LocationId.Game, "Launcher/LariLauncher.exe");
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { Bg3Constants.ModsLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/Mods") },
            { Bg3Constants.PlayerProfilesLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/PlayerProfiles/Public") },
            { Bg3Constants.ScriptExtenderConfigLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/ScriptExtender") },
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

    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.tile.webp");
}
