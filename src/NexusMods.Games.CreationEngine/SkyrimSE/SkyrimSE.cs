using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
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

public partial class SkyrimSE : AGame, ISteamGame, IGogGame, ICreationEngineGame, IGameData<SkyrimSE>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDiagnosticEmitter[] _emitters;
    private readonly IStreamSourceDispatcher _streamSource;

    public static GameId GameId { get; } = GameId.From("CreationEngine.SkyrimSE");
    protected override GameId GameIdImpl => GameId;

    public static string DisplayName => "Skyrim Special Edition";
    protected override string DisplayNameImpl => DisplayName;

    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(1704);
    protected override Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameIdImpl => NexusModsGameId;

    public override StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [489830u],
        GOGProductIds = [1711230643L],
    };

    public IEnumerable<uint> SteamIds => StoreIdentifiers.SteamAppIds;
    public IEnumerable<long> GogIds => StoreIdentifiers.GOGProductIds;

    public SkyrimSE(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _streamSource = provider.GetRequiredService<IStreamSourceDispatcher>();

        _emitters =
        [
            new MissingMasterEmitter(this),
        ];
    }

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "SkyrimSE.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        string postfix = "";
        if (installation.Store == GameStore.GOG)
            postfix = " GOG";
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory) / "Skyrim Special Edition" },
            { LocationId.Preferences, fileSystem.GetKnownPath(KnownPath.MyGamesDirectory) / ("Skyrim Special Edition" + postfix)},
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        return [];
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider) => new SkyrimSESynchronizer(provider, this);



    public override IStreamFactory IconImage => new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.thumbnail.webp");
    public override IStreamFactory TileImage => new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.tile.webp");

    public override IDiagnosticEmitter[] DiagnosticEmitters => _emitters;

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "Data")),
        // Files in a Data folder
        new StopPatternInstaller(_serviceProvider)
        {
            GameId = GameId,
            GameAliases = ["Skyrim Special Edition", "SkyrimSE", "SSE"],
            TopLevelDirs = KnownPaths.CommonTopLevelFolders,
            StopPatterns = ["(^|/)skse(/|$)"],
            EngineFiles = [@"skse64_loader\.exe", @"skse64_.*\.dll"],
            
        }.Build(),
    ];

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

    public GamePath PluginsFile => new GamePath(LocationId.AppData, "Plugins.txt");
}
