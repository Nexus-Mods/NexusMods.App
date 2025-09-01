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

public class SkyrimSE : AGame, ISteamGame, IGogGame
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

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "")),
        new GenericPatternMatchInstaller(_serviceProvider)
        {
            InstallFolderTargets =
            [
                // Script Extender
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, RelativePath.Empty),
                    KnownValidFileExtensions = [KnownExtensions.Exe],
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt,
                    ],
                },
                new InstallFolderTarget
                {
                    KnownSourceFolderNames = ["SKSE"],
                    DestinationGamePath = new GamePath(LocationId.Game, "Data/SKSE"),
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt,
                    ],
                },
                new InstallFolderTarget
                {
                    KnownSourceFolderNames = ["Data"],
                    DestinationGamePath = new GamePath(LocationId.Game, "Data"),
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt,
                    ],
                },
                new InstallFolderTarget
                {
                    KnownSourceFolderNames = ["Interface"],
                    DestinationGamePath = new GamePath(LocationId.Game, "Data/Interface"),
                },
                new InstallFolderTarget
                {
                    KnownSourceFolderNames = ["meshes"],
                    DestinationGamePath = new GamePath(LocationId.Game, "Data/meshes"),
                },
                new InstallFolderTarget
                {
                    KnownValidFileExtensions = [KnownCEExtensions.BSA, KnownCEExtensions.BA2, KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL],
                    DestinationGamePath = new GamePath(LocationId.Game, "Data"),
                },
                new InstallFolderTarget
                {
                    SubTargets = [
                        new InstallFolderTarget()
                        {
                            KnownSourceFolderNames = ["Scripts"],
                            DestinationGamePath = new GamePath(LocationId.Game, "Data/Scripts"),
                        },
                        new InstallFolderTarget()
                        {
                            KnownSourceFolderNames = ["Source"],
                            DestinationGamePath = new GamePath(LocationId.Game, "Data/Source"),
                            
                        }
                    ]
                },
                
            ],
        },

    ];
}
