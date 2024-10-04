using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Obsidian.FalloutNewVegas.Emitters;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

// The argument could be made that the package should be Bethesda not Obsidian... todo someone confirm preferred package name
namespace NexusMods.Games.Obsidian.FalloutNewVegas;

public class FalloutNewVegas : AGame, ISteamGame, IGogGame, IXboxGame, IEpicGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOSInformation _osInformation;

    public FalloutNewVegas(IServiceProvider provider, IServiceProvider serviceProvider, IOSInformation osInformation) : base(provider)
    {
        _serviceProvider = serviceProvider;
        _osInformation = osInformation;
    }
    
    private static readonly string _name = "Fallout New Vegas";

    public static string GameName => _name; // used to statically reference the game name elsewhere... in case the name changes? idk.
    public override string Name => _name;
    public override GameDomain Domain => GameDomain.From("newvegas");

    #region File Information

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
        return result;
    }
    
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return store.ToString() switch
        {
            "Epic Games Store" => new GamePath(LocationId.Game, "/Fallout New Vegas English/FalloutNV.exe"), // todo going to need this to handle the language... somehow?
            _ => new GamePath(LocationId.Game, "FalloutNV.exe"),
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) => ModInstallDestinationHelpers.GetCommonLocations(locations);

    #endregion

    #region Game IDs

    public IEnumerable<uint> SteamIds => new List<uint> { 22380u };
    public IEnumerable<long> GogIds => new List<long> { 1207658921 }; //todo need correct ID. I don't own this.
    public IEnumerable<string> XboxIds => new List<string> { "9P4P6BZQ9V6M" }; //todo need correct ID
    public IEnumerable<string> EpicCatalogItemId => new List<string> { "dabb52e328834da7bbe99691e374cb84" };
    #endregion

    #region Images

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<FalloutNewVegas>("NexusMods.Games.Obsidian.Resources.FalloutNewVegas.game_image.jpg");
    public override IStreamFactory Icon => new EmbededResourceStreamFactory<FalloutNewVegas>("NexusMods.Games.Obsidian.Resources.FalloutNewVegas.icon.jpg");

    #endregion


    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new GenericPatternMatchInstaller(_serviceProvider)
        {
            InstallFolderTargets =
            [
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, "Data"),
                    KnownSourceFolderNames = ["Data"],
                    Names = ["meshes", "textures", "sound", "nvse", "music", "video", "menus", "shaders"],
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt, KnownExtensions.Md, KnownExtensions.Pdf, KnownExtensions.Png,
                        KnownExtensions.Json, new Extension(".lnk"),
                    ],
                },
            ],
        },
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "Data")),
    ];

    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        new MissingNVSEEmitter(),
    ];

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new FalloutNewVegasSynchronizer(provider);
    }
}
