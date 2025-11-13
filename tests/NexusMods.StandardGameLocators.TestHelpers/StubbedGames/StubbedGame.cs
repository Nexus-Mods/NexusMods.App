using System.Collections.Immutable;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileExtractor;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.IO;

// ReSharper disable InconsistentNaming

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGame : IGame, IGameData<StubbedGame>
{
    private readonly ILogger<StubbedGame> _logger;
    private readonly IEnumerable<IGameLocator> _locators;

    public static GameId GameId { get; } = GameId.From("StubbedGame");
    public static string DisplayName => "Stubbed Game";

    // TODO: make None after moving to GameId
    public static Optional<Sdk.NexusModsApi.NexusModsGameId> NexusModsGameId => Sdk.NexusModsApi.NexusModsGameId.From(uint.MaxValue);

    public StoreIdentifiers StoreIdentifiers { get; } = new(GameId)
    {
        SteamAppIds = [42u],
        GOGProductIds = [42L],
        EADesktopSoftwareIds = ["ea-game-id"],
        EGSCatalogItemId = ["epic-game-id"],
        OriginManifestIds = ["origin-game-id"],
        XboxPackageIdentifiers = ["xbox-game-id"],
    };

    public IStreamFactory IconImage => new EmbeddedResourceStreamFactory<StubbedGame>("NexusMods.StandardGameLocators.TestHelpers.Resources.question_mark_game.png");
    public IStreamFactory TileImage => throw new NotImplementedException("No game image for stubbed game.");

    private readonly IServiceProvider _serviceProvider;
    public StubbedGame(ILogger<StubbedGame> logger, IEnumerable<IGameLocator> locators, IFileSystem fileSystem, IServiceProvider provider)
    {
        _serviceProvider = provider;
        _logger = logger;
        _locators = locators;
    }

    public GamePath GetPrimaryFile(GameInstallation installation) => new(LocationId.Game, "");

    public ILoadoutSynchronizer Synchronizer => new DefaultSynchronizer(_serviceProvider);

    public ImmutableDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult gameLocatorResult)
    {
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, gameLocatorResult.Path.Combine("game")},
            { LocationId.Preferences, gameLocatorResult.Path.Combine("preferences")},
            { LocationId.Saves, gameLocatorResult.Path.Combine("saves")},
        }.ToImmutableDictionary();
    }

    public ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new StubbedGameInstaller(_serviceProvider),
    ];

    public IDiagnosticEmitter[] DiagnosticEmitters => [];
    public ISortOrderManager SortOrderManager => new SortOrderManager(_serviceProvider);

    /// <summary>
    /// Incremented version number for each new game.
    /// </summary>
    private static int NextId = 0;
    
    /// <summary>
    /// Create a new stubbed game installation, in its own temporary folder, with a unique version number.
    /// </summary>
    public static async Task<GameInstallation> Create(IServiceProvider provider)
    {
        var tmpFileManager = provider.GetRequiredService<TemporaryFileManager>();
        var game = provider.GetRequiredService<StubbedGame>();
        var version = Interlocked.Increment(ref NextId);
        var path = tmpFileManager.CreateFolder();

        {
            using var tx = provider.GetRequiredService<IConnection>().BeginTransaction();
            _ = new ManuallyAddedGame.New(tx)
            {
                Path = path.ToString(),
                Version = Version.Parse($"1.{version}.0.0").ToString(),
                GameId = NexusModsGameId.Value,
            };
            await tx.Commit();
        }

        await AddTestFiles(path, provider);

        var gameRegistry = provider.GetRequiredService<IGameRegistry>();
        var install = gameRegistry.LocateGameInstallations().First(g => g.Game is StubbedGame && g.LocatorResult.Path == path);

        return install;
    }

    private static async Task AddTestFiles(TemporaryPath path, IServiceProvider provider)
    {
        var stateFolder = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/StubbedGameState.zip");
        var extractor = provider.GetRequiredService<IFileExtractor>();
        
        await extractor.ExtractAllAsync(stateFolder, path, CancellationToken.None);

    }
}
