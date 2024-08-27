using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.FOMOD;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

[UsedImplicitly]
public class Cyberpunk2077Game : AGame, ISteamGame, IGogGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("cyberpunk2077");
    private readonly IServiceProvider _serviceProvider;

    public Cyberpunk2077Game(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
        => new Cyberpunk2077Synchronizer(provider);

    public override string Name => "Cyberpunk 2077";
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "bin/x64/Cyberpunk2077.exe");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            {
                LocationId.Saves,
                fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Saved Games/CD Projekt Red/Cyberpunk 2077")
            },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine("CD Projekt Red/Cyberpunk 2077")
            }
        };

        return result;
    }

    public IEnumerable<uint> SteamIds => new[] { 1091500u };
    public IEnumerable<long> GogIds => new[] { 2093619782L, 1423049311 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "5beededaad9743df90e8f07d92df153f" };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.game_image.jpg");
    
    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        new PatternBasedDependencyEmitter(PatternDefinitions.Definitions, _serviceProvider),
    ];
    
    /// <inheritdoc />
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new RedModInstaller(_serviceProvider),
        new SimpleOverlayModInstaller(_serviceProvider),
        new AppearancePresetInstaller(_serviceProvider),
        new FolderlessModInstaller(_serviceProvider),
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "/")),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
