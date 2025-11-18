using System.Collections.Immutable;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.FileHashes.Emitters;
using NexusMods.Games.FOMOD;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

[UsedImplicitly]
public class Cyberpunk2077Game : IGame, IGameData<Cyberpunk2077Game>
{
    public static GameId GameId { get; } = GameId.From("RedEngine.Cyberpunk2077");
    public static string DisplayName => "Cyberpunk 2077";
    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(3333);

    public StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [1091500u],
        GOGProductIds = [2093619782L, 1423049311L],
        EGSCatalogItemId = ["5beededaad9743df90e8f07d92df153f"],
    };

    public IStreamFactory IconImage { get; } = new EmbeddedResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.thumbnail.webp");
    public IStreamFactory TileImage { get; } = new EmbeddedResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.tile.webp");

    private readonly Lazy<ILoadoutSynchronizer> _synchronizer;
    public ILoadoutSynchronizer Synchronizer => _synchronizer.Value;
    public ILibraryItemInstaller[] LibraryItemInstallers { get; }
    private readonly Lazy<ISortOrderManager> _sortOrderManager;
    public ISortOrderManager SortOrderManager => _sortOrderManager.Value;
    public IDiagnosticEmitter[] DiagnosticEmitters { get; }

    public Cyberpunk2077Game(IServiceProvider provider)
    {
        _synchronizer = new Lazy<ILoadoutSynchronizer>(() => new Cyberpunk2077Synchronizer(provider));
        _sortOrderManager = new Lazy<ISortOrderManager>(() =>
        {
            var sortOrderManager = provider.GetRequiredService<SortOrderManager>();
            sortOrderManager.RegisterSortOrderVarieties(
                sortOrderVarieties: [
                    provider.GetRequiredService<RedModSortOrderVariety>(),
                ],
                game: this
            );

            return sortOrderManager;
        });

        DiagnosticEmitters =
        [
            new NoWayToSourceFilesOnDisk(),
            new UndeployableLoadoutDueToMissingGameFiles(provider),
            new PatternBasedDependencyEmitter(PatternDefinitions.Definitions, provider),
            new MissingProtontricksForRedModEmitter(provider),
            new MissingRedModEmitter(),
            new WinePrefixRequirementsEmitter(),
        ];

        LibraryItemInstallers =
        [
            FomodXmlInstaller.Create(provider, new GamePath(LocationId.Game, "")),
            new RedModInstaller(provider),
            new SimpleOverlayModInstaller(provider),
            new AppearancePresetInstaller(provider),
            new FolderlessModInstaller(provider),
        ];
    }

    public ImmutableDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult gameLocatorResult)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, gameLocatorResult.Path },
            // Skip managing saves for now, to prevent accidental deletion of saves
            // e.g. when removing loadouts, un-managing the game, or uninstalling the app
            // {
            //     LocationId.Saves,
            //     fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Saved Games/CD Projekt Red/Cyberpunk 2077")
            // },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("CD Projekt Red/Cyberpunk 2077")
            }
        }.ToImmutableDictionary();
    }

    public GamePath GetPrimaryFile(GameInstallation installation) => new(LocationId.Game, "bin/x64/Cyberpunk2077.exe");
}
