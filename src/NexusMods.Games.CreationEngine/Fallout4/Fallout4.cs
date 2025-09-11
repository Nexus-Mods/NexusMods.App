using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.CreationEngine.Installers;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.Fallout4;

public partial class Fallout4 : AGame, ISteamGame, IGogGame, ICreationEngineGame
{
    private readonly IServiceProvider _serviceProvider;

    public Fallout4(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
    }

    public override string Name => "Fallout 4";
    public override GameId GameId => GameId.From(1151);
    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "Fallout4.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
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
    
    [GeneratedRegex("f4se_\\d+_\\d+_\\d+", RegexOptions.IgnoreCase)]
    private static partial Regex F4seRegex();
    
    
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

    public GamePath PluginsFile => Fallout4KnownPaths.PluginsFile;
}
