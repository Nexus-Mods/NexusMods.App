using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.CreationEngine.Emitters;
using NexusMods.Games.CreationEngine.Installers;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.Fallout4;

public partial class Fallout4 : AGame, ISteamGame, IGogGame, ICreationEngineGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDiagnosticEmitter[] _emitters;
    private readonly IStreamSourceDispatcher _streamSource;

    public Fallout4(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _streamSource = provider.GetRequiredService<IStreamSourceDispatcher>();
        
        _emitters =
        [
            new MissingMasterEmitter(this),
        ];
    }

    public override string Name => "Fallout 4";
    public override GameId GameId => GameId.From(1151);
    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "Fallout4.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory) / "Fallout4" },
            { LocationId.Preferences, fileSystem.GetKnownPath(KnownPath.MyGamesDirectory) / "Fallout4" },
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        return [];
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider) => new Fallout4Synchronizer(provider, this);

    public override SupportType SupportType => SupportType.Unsupported;
    public IEnumerable<uint> SteamIds => [377160];
    public IEnumerable<long> GogIds => [ 1998527297 ];
        
    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<Fallout4>("NexusMods.Games.CreationEngine.Resources.Fallout4.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<Fallout4>("NexusMods.Games.CreationEngine.Resources.Fallout4.tile.webp");

    
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "Data")),
        new StopPatternInstaller(_serviceProvider)
        {
            GameId = GameId,
            GameAliases = ["Fallout 4", "Fallout4", "FO4", "F4"],
            TopLevelDirs = KnownPaths.CommonTopLevelFolders,
            StopPatterns = ["(^|/)f4se(/|$)"],
            EngineFiles = [
                // F4SE
                @"f4se_loader\.exe", 
                @"f4se_.*\.dll",
                // Plugin Preloader (new
                @"winhttp\.dll",
                @"xSE\ PluginPreloader\.xml",
                // Plugin Preloader (old)
                @"IpHlpAPI\.dll",
            ],
        }.Build(),
    ];
    
    public override IDiagnosticEmitter[] DiagnosticEmitters => _emitters;

    private static readonly GroupMask EmptyGroupMask = new(false);
    public async ValueTask<IMod?> ParsePlugin(Hash hash, RelativePath? name = null)
    {
        var fileName = name?.FileName.ToString() ?? "unknown.esm";
        var key = ModKey.FromFileName(fileName);
        await using var stream = await _streamSource.OpenAsync(hash);
        var meta = ParsingMeta.Factory(BinaryReadParameters.Default, GameRelease.Fallout4, key, stream!);
        await using var mutagenStream = new MutagenBinaryReadStream(stream!, meta);
        using var frame = new MutagenFrame(mutagenStream);
        return Fallout4Mod.CreateFromBinary(frame, Fallout4Release.Fallout4, EmptyGroupMask);
    }

    public GamePath PluginsFile => Fallout4KnownPaths.PluginsFile;
}
