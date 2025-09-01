using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic.Installers;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.Fallout4;

public partial class Fallout4 : AGame, ISteamGame, IGogGame
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

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider) => new Fallout4Synchronizer(provider);

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
        // Files in a Data folder
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.ThisNameIs("Data"),
            Destination = KnownPaths.Data,
            IgnoreFiles = ["fomod/info.xml"],  
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.HasDirectChild("winhttp.dll") || n.HasDirectChild("iphlpapi.dll"),
            Destination = KnownPaths.Game,
        },
        // SKSE wraps its files in a folder named after the skse version
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.ThisNameLike(F4seRegex()),
            Destination = KnownPaths.Game,
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.HashDirectChildrenWith(KnownCEExtensions.BA2, KnownCEExtensions.ESM, KnownCEExtensions.ESL, KnownCEExtensions.ESP),
            Destination = KnownPaths.Data,
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.ThisNameIs("F4SE"),
            Destination = new GamePath(LocationId.Game, "Data/F4SE"),
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.HasAnyDirectChildFolder("meshes", "textures", "Interface", "F4SE", "sound", "scripts", "MCM"), 
            Destination = KnownPaths.Data, 
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.HasDirectChildEndingIn("_SWAP.ini"),
            Destination = KnownPaths.Data,
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.IsRoot && n.HasDirectChild("Tools"),
            Destination = KnownPaths.Game,
        }
    ];
}
