using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.FOMOD;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

[UsedImplicitly]
public class Cyberpunk2077Game : AGame, ISteamGame, IGogGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("cyberpunk2077");
    public static GameId GameIdStatic => GameId.From(3333);
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private ISortableItemProviderFactory[] _sortableItemProviderFactories;

    public Cyberpunk2077Game(IServiceProvider provider, IConnection connection) : base(provider)
    {
        _serviceProvider = provider;
        _connection = connection;
        
        _sortableItemProviderFactories =
        [
            _serviceProvider.GetRequiredService<RedModSortableItemProviderFactory>(),
        ];
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
        => new Cyberpunk2077Synchronizer(provider);

    public override string Name => "Cyberpunk 2077";
    public override GameId GameId => GameIdStatic;
    public override SupportType SupportType => SupportType.Official;

    public override HashSet<FeatureStatus> Features { get; } =
    [
        new(BaseFeatures.GameLocatable, IsImplemented: true),
        new(BaseFeatures.HasInstallers, IsImplemented: true),
        new(BaseFeatures.HasDiagnostics, IsImplemented: true),
        new(BaseFeatures.HasLoadOrder, IsImplemented: false),
    ];

    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "bin/x64/Cyberpunk2077.exe");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            // Skip managing saves for now, to prevent accidental deletion of saves
            // e.g. when removing loadouts, un-managing the game, or uninstalling the app
            // {
            //     LocationId.Saves,
            //     fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Saved Games/CD Projekt Red/Cyberpunk 2077")
            // },
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
        new MissingProtontricksForRedModEmitter(_serviceProvider),
        new MissingRedModEmitter(),
    ];

    public override ISortableItemProviderFactory[] SortableItemProviderFactories => _sortableItemProviderFactories;
    
    /// <inheritdoc />
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "")),
        new RedModInstaller(_serviceProvider),
        new SimpleOverlayModInstaller(_serviceProvider),
        new AppearancePresetInstaller(_serviceProvider),
        new FolderlessModInstaller(_serviceProvider),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
