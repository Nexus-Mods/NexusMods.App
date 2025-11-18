using System.Collections.Immutable;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Larian.BaldursGate3.Emitters;
using NexusMods.Games.Larian.BaldursGate3.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3 : IGame, IGameData<BaldursGate3>
{
    public static GameId GameId { get; } = GameId.From("Larian.BaldursGate3");
    public static string DisplayName => "Baldur's Gate 3";
    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(3474);

    public StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [1086940u],
        GOGProductIds = [1456460669L],
    };

    public IStreamFactory IconImage { get; } = new EmbeddedResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.thumbnail.webp");
    public IStreamFactory TileImage { get; } = new EmbeddedResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.tile.webp");

    private readonly Lazy<ILoadoutSynchronizer> _synchronizer;
    public ILoadoutSynchronizer Synchronizer => _synchronizer.Value;
    public ILibraryItemInstaller[] LibraryItemInstallers { get; }
    private readonly Lazy<ISortOrderManager> _sortOrderManager;
    public ISortOrderManager SortOrderManager => _sortOrderManager.Value;
    public IDiagnosticEmitter[] DiagnosticEmitters { get; }

    public BaldursGate3(IServiceProvider provider)
    {
        _synchronizer = new Lazy<ILoadoutSynchronizer>(() => new BaldursGate3Synchronizer(provider));
        _sortOrderManager = new Lazy<ISortOrderManager>(() =>
        {
            var sortOrderManager = provider.GetRequiredService<SortOrderManager>();
            sortOrderManager.RegisterSortOrderVarieties([], this);
            return sortOrderManager;
        });

        DiagnosticEmitters = [provider.GetRequiredService<DependencyDiagnosticEmitter>()];

        LibraryItemInstallers =
        [
            new BG3SEInstaller(provider),
            new GenericPatternMatchInstaller(provider)
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
    }

    public ImmutableDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult gameLocatorResult)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, gameLocatorResult.Path },
            { Bg3Constants.ModsLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/Mods") },
            { Bg3Constants.PlayerProfilesLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/PlayerProfiles/Public") },
            { Bg3Constants.ScriptExtenderConfigLocationId, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/ScriptExtender") },
        }.ToImmutableDictionary();
    }

    public GamePath GetPrimaryFile(GameInstallation installation)
    {
        if (installation.LocatorResult.TargetOS.IsOSX) return new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3");

        // Use launcher to allow choosing between DirectX11 and Vulkan on GOG, Steam already always starts the launcher
        return new GamePath(LocationId.Game, "Launcher/LariLauncher.exe");
    }
}
