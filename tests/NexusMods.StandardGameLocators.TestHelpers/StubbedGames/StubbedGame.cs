using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EADesktop;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Origin;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;
using NexusMods.Sdk.FileExtractor;
using NexusMods.Sdk.IO;

// ReSharper disable InconsistentNaming

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGame : AGame, IEADesktopGame, IEpicGame, IOriginGame, ISteamGame, IGogGame, IXboxGame
{
    private readonly ILogger<StubbedGame> _logger;
    private readonly IEnumerable<IGameLocator> _locators;
    public override string DisplayName => "Stubbed Game";
    public override Optional<Sdk.NexusModsApi.GameId> NexusModsGameId => Sdk.NexusModsApi.GameId.From(uint.MaxValue);

    private readonly IServiceProvider _serviceProvider;
    public StubbedGame(ILogger<StubbedGame> logger, IEnumerable<IGameLocator> locators,
        IFileSystem fileSystem, IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _logger = logger;
        _locators = locators;
    }

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "");

    public override ILoadoutSynchronizer Synchronizer =>
        // Lazy initialization to avoid circular dependencies
        new DefaultSynchronizer(_serviceProvider);

    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<StubbedGame>(
            "NexusMods.StandardGameLocators.TestHelpers.Resources.question_mark_game.png");

    public override IStreamFactory GameImage => throw new NotImplementedException("No game image for stubbed game.");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, installation.Path.Combine("game")},
            { LocationId.Preferences, installation.Path.Combine("preferences")},
            { LocationId.Saves, installation.Path.Combine("saves")},
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) => new();
    

    public IEnumerable<uint> SteamIds => [42u];
    public IEnumerable<long> GogIds => [42];
    public IEnumerable<string> EADesktopSoftwareIDs => ["ea-game-id"];
    public IEnumerable<string> EpicCatalogItemId => ["epic-game-id"];
    public IEnumerable<string> OriginGameIds => ["origin-game-id"];
    public IEnumerable<string> XboxIds => ["xbox-game-id"];
    
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new StubbedGameInstaller(_serviceProvider),
    ];

    /// <summary>
    /// Incremented version number for each new game.
    /// </summary>
    private static int NextId = 0;
    
    /// <summary>
    /// Create a new stubbed game installation, in its own temporary folder, with a unique version number.
    /// </summary>
    public static async Task<GameInstallation> Create(IServiceProvider provider)
    {
        var locator = provider.GetServices<IGameLocator>().OfType<ManuallyAddedLocator>().First();
        var tmpFileManager = provider.GetRequiredService<TemporaryFileManager>();
        var game = provider.GetRequiredService<StubbedGame>();
        var version = Interlocked.Increment(ref NextId);

        var path = tmpFileManager.CreateFolder();

        var (id, install) = await locator.Add(game, Version.Parse($"1.{version}.0.0"), path);

        await AddTestFiles(path, provider);
        
        return install;
    }

    private static async Task AddTestFiles(TemporaryPath path, IServiceProvider provider)
    {
        var stateFolder = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/StubbedGameState.zip");
        var extractor = provider.GetRequiredService<IFileExtractor>();
        
        await extractor.ExtractAllAsync(stateFolder, path, CancellationToken.None);

    }
}
