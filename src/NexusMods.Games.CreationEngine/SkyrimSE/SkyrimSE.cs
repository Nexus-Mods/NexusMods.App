using System.Collections.Immutable;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.CreationEngine.Emitters;
using NexusMods.Games.CreationEngine.Installers;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public class SkyrimSE : ICreationEngineGame, IGameData<SkyrimSE>
{
    private readonly IStreamSourceDispatcher _streamSource;

    public static GameId GameId { get; } = GameId.From("CreationEngine.SkyrimSE");
    public static string DisplayName => "Skyrim Special Edition";
    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(1704);

    public StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [489830u],
        GOGProductIds = [1711230643L],
    };

    public IStreamFactory IconImage => new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.thumbnail.webp");
    public IStreamFactory TileImage => new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.tile.webp");

    private readonly Lazy<ILoadoutSynchronizer> _synchronizer;
    public ILoadoutSynchronizer Synchronizer => _synchronizer.Value;
    public ILibraryItemInstaller[] LibraryItemInstallers { get; }
    private readonly Lazy<ISortOrderManager> _sortOrderManager;
    public ISortOrderManager SortOrderManager => _sortOrderManager.Value;
    public IDiagnosticEmitter[] DiagnosticEmitters { get; }
    
    public SkyrimSE(IServiceProvider provider)
    {
        _streamSource = provider.GetRequiredService<IStreamSourceDispatcher>();

        _synchronizer = new Lazy<ILoadoutSynchronizer>(() => new SkyrimSESynchronizer(provider, this));
        _sortOrderManager = new Lazy<ISortOrderManager>(() =>
        {
            var sortOrderManager = provider.GetRequiredService<SortOrderManager>();
            sortOrderManager.RegisterSortOrderVarieties([], this);
            return sortOrderManager;
        });

        DiagnosticEmitters =
        [
            new MissingMasterEmitter(this),
        ];

        LibraryItemInstallers =
        [
            FomodXmlInstaller.Create(provider, new GamePath(LocationId.Game, "Data")),
            // Files in a Data folder
            new StopPatternInstaller(provider)
            {
                GameId = GameId,
                GameAliases = ["Skyrim Special Edition", "SkyrimSE", "SSE"],
                TopLevelDirs = KnownPaths.CommonTopLevelFolders,
                StopPatterns = ["(^|/)skse(/|$)"],
                EngineFiles = [@"skse64_loader\.exe", @"skse64_.*\.dll"],
                
            }.Build(),
        ];
    }

    public ImmutableDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult gameLocatorResult)
    {
        var postfix = "";
        if (gameLocatorResult.Store == GameStore.GOG) postfix = " GOG";

        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, gameLocatorResult.Path },
            { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory) / "Skyrim Special Edition" },
            { LocationId.Preferences, fileSystem.GetKnownPath(KnownPath.MyGamesDirectory) / ("Skyrim Special Edition" + postfix)},
        }.ToImmutableDictionary();
    }

    public GamePath GetPrimaryFile(GameInstallation installation) => new(LocationId.Game, "SkyrimSE.exe");

    private static readonly GroupMask EmptyGroupMask = new(false);
    public async ValueTask<IMod?> ParsePlugin(Hash hash, RelativePath? name = null)
    {
        var fileName = name?.FileName.ToString() ?? "unknown.esm";
        var key = ModKey.FromFileName(fileName);
        await using var stream = await _streamSource.OpenAsync(hash);
        var meta = ParsingMeta.Factory(BinaryReadParameters.Default, GameRelease.SkyrimSE, key, stream!);
        await using var mutagenStream = new MutagenBinaryReadStream(stream!, meta);
        using var frame = new MutagenFrame(mutagenStream);
        return SkyrimMod.CreateFromBinary(frame, SkyrimRelease.SkyrimSE, EmptyGroupMask);
    }

    public GamePath PluginsFile => new(LocationId.AppData, "Plugins.txt");
}
