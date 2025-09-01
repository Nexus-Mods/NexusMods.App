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
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public partial class SkyrimSE : AGame, ISteamGame, IGogGame
{
    private readonly IServiceProvider _serviceProvider;

    public SkyrimSE(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
    }

    public override string Name => "Skyrim Special Edition";
    public override GameId GameId => GameId.From(1704);
    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "SkyrimSE.exe");

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
    
    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider) => new SkyrimSESynchronizer(provider);

    public override SupportType SupportType => SupportType.Unsupported;
    public IEnumerable<uint> SteamIds => [489830];
    public IEnumerable<long> GogIds => [ 1711230643 ];
    
    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<SkyrimSE>("NexusMods.Games.CreationEngine.Resources.SkyrimSE.tile.webp");

    [GeneratedRegex("skse64_\\d+_\\d+_\\d+", RegexOptions.IgnoreCase)]
    private static partial Regex SkseRegex();
    
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "")),
        // Files in a Data folder
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.ThisNameIs("Data"),
            Destination = new GamePath(LocationId.Game, "Data"),
        },
        // SKSE wraps its files in a folder named after the skse version
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.ThisNameLike(SkseRegex()),
            Destination = new GamePath(LocationId.Game, ""),
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.IsRootWith(KnownCEExtensions.BSA, KnownCEExtensions.ESM, KnownCEExtensions.ESL, KnownCEExtensions.ESP),
            Destination = new GamePath(LocationId.Game, "Data"),
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.ThisNameIs("Interface"), 
            Destination = new GamePath(LocationId.Game, "Data/Interface"), 
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.ThisNameIs("meshes"), 
            Destination = new GamePath(LocationId.Game, "Data/meshes"), 
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.ThisNameIs("textures"), 
            Destination = new GamePath(LocationId.Game, "Data/textures"), 
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.ThisNameIs("sound"), 
            Destination = new GamePath(LocationId.Game, "Data/sound"), 
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.HasDirectChildFolder("scripts"), 
            Destination = new GamePath(LocationId.Game, "Data"), 
        },
        new PredicateBasedInstaller(_serviceProvider) 
        { 
            Root = static n => n.HasDirectChildFolder("meshes") || n.HasDirectChildFolder("textures") || n.HasDirectChildFolder("SKSE"), 
            Destination = new GamePath(LocationId.Game, "Data"), 
        },
        new PredicateBasedInstaller(_serviceProvider)
        {
            Root = static n => n.HasDirectChildEndingIn("_SWAP.ini"),
            Destination = new GamePath(LocationId.Game, "Data"),
        }
    ];
}
