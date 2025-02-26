using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Tests;

/// <summary>
///     Tests centered around the <see cref="ModUpdateService" />.
///     Including:
///     - Detection of incoming library items.
///     - Detection of removed library items.
///     - Refresh from clean state.
///     - Correct firing of observables (page, mod).
///     - Correct result of updates.
/// </summary>
[Trait("RequiresNetworking", "True")]
public class ModUpdateServiceTests
{
    private readonly IConnection _connection;
    private readonly IGameDomainToGameIdMappingCache _gameIdMappingCache;
    private readonly NexusGraphQLClient _gqlClient;
    private readonly ILibraryService _libraryService;
    private readonly ILogger<ModUpdateService> _logger;
    private readonly IModUpdateService _modUpdateService;
    private readonly INexusApiClient _nexusApiClient;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly FakeTimeProvider _timeProvider;

    public ModUpdateServiceTests(
        IConnection connection,
        INexusApiClient nexusApiClient,
        IGameDomainToGameIdMappingCache gameIdMappingCache,
        ILogger<ModUpdateService> logger,
        NexusGraphQLClient gqlClient,
        TemporaryFileManager temporaryFileManager,
        ILibraryService libraryService,
        NexusModsLibrary nexusModsLibrary)
    {
        _connection = connection;
        _nexusApiClient = nexusApiClient;
        _gameIdMappingCache = gameIdMappingCache;
        _logger = logger;
        _gqlClient = gqlClient;
        _temporaryFileManager = temporaryFileManager;
        _libraryService = libraryService;
        _nexusModsLibrary = nexusModsLibrary;
        _timeProvider = new FakeTimeProvider();
        _modUpdateService = new ModUpdateService(
            _connection,
            _nexusApiClient,
            _gameIdMappingCache,
            _logger,
            _gqlClient,
            _timeProvider);
    }

    [Fact]
    public async Task CheckAndUpdateModPages_WithNoMods_ShouldReturnEmptyResult()
    {
        // Act
        var result = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            true,
            false
        );

        // Assert
        result.IsEmpty().Should().BeTrue();
        result.ResultStatus.Should().Be(CacheUpdaterResultStatus.Ok);
        // Ignore if you're somehow throttled.
    }

    [Fact]
    public async Task CheckAndUpdateModPages_WithThrottleTrue_ShouldRespectCooldown()
    {
        // First call
        _ = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: true
        );

        // Second immediate call should be rate-limited
        var secondResult = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: true
        );

        // The second call should be rate limited
        secondResult.ResultStatus.Should().Be(CacheUpdaterResultStatus.Throttled);
    }

    [Fact]
    public async Task CheckAndUpdateModPages_WithThrottleTrue_ShouldAllowAfterCooldown()
    {
        // First call
        _ = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: true
        );

        // Advance time past the cooldown
        _timeProvider.Advance(TimeSpan.FromSeconds(ModUpdateService.UpdateCheckCooldownSeconds + 1));

        // Second call after cooldown should not be rate-limited
        var secondResult = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: true
        );

        // The second call should not be rate limited
        secondResult.ResultStatus.Should().Be(CacheUpdaterResultStatus.Ok);
    }

    [Fact]
    public async Task CheckAndUpdateModPages_WithThrottleFalse_ShouldIgnoreCooldown()
    {
        // First call
        _ = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: true
        );

        // Second immediate call with throttle=false should not be rate-limited
        var secondResult = await _modUpdateService.CheckAndUpdateModPages(
            CancellationToken.None,
            notify: false,
            throttle: false
        );

        // The second call should not be rate limited because throttle is false
        secondResult.ResultStatus.Should().Be(CacheUpdaterResultStatus.Ok);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var modUpdateService = new ModUpdateService(_connection, _nexusApiClient, _gameIdMappingCache,
            _logger, _gqlClient, _timeProvider
        );

        // Act & Assert
        var action = () => ((IDisposable)modUpdateService).Dispose();
        action.Should().NotThrow();
    }
}

/// <summary>
/// Static data to test against at test time.
/// </summary>
internal static class StaticTestData
{
    /// <summary>
    /// Model for an item to test with.
    /// </summary>
    internal struct TestModData
    {
        /// <summary>
        /// Name of the item on Nexus Mods
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// 32-bit FileID
        /// </summary>
        public uint FileId { get; init; }

        /// <summary>
        /// 32-bit GameID
        /// </summary>
        public uint GameId { get; init; }

        /// <summary>
        /// Version as attached to the file to Nexus.
        /// </summary>
        public string Version { get; init; }

        /// <summary>
        /// Expected updates for this file.
        /// These are not all the updates, only most of them.
        /// </summary>
        public TestModData[] Updates { get; init; }
    }

    public static readonly TestModData SmapiModData = new()
    {
        Name = "SMAPI 4.0.8",
        FileId = 5001,
        GameId = 1303,
        Version = "4.0.8",
        Updates =
        [
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.1",
                FileId = 109556,
                GameId = 1303,
                Version = "4.1.0-beta.1",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.2",
                FileId = 110769,
                GameId = 1303,
                Version = "4.1.0-beta.2",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.3",
                FileId = 112400,
                GameId = 1303,
                Version = "4.1.0-beta.3",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.4",
                FileId = 112827,
                GameId = 1303,
                Version = "4.1.0-beta.4",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.5",
                FileId = 113649,
                GameId = 1303,
                Version = "4.1.0-beta.5",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0",
                FileId = 115051,
                GameId = 1303,
                Version = "4.1.0",
            },
            new TestModData
            {
                Name = "SMAPI 4.1.1",
                FileId = 115121,
                GameId = 1303,
                Version = "4.1.1",
            },
        ],
    };


    public static readonly TestModData SpaceCoreModData = new()
    {
        Name = "1.2-SpaceCore-1.0.1.zip",
        FileId = 94412,
        GameId = 1303,
        Version = "1.0.1",
        Updates = new TestModData[]
        {
            new()
            {
                Name = "1.2-SpaceCore-1.0.2.zip",
                FileId = 5018,
                GameId = 1303,
                Version = "1.0.2",
            },
            new()
            {
                Name = "1.2-SpaceCore-1.0.3.zip",
                FileId = 5071,
                GameId = 1303,
                Version = "1.0.3",
            },
            new()
            {
                Name = "1.2-SpaceCore-1.0.4.zip",
                FileId = 6066,
                GameId = 1303,
                Version = "1.0.4",
            },
            new()
            {
                Name = "1.2-SpaceCore-1.0.5.zip",
                FileId = 6077,
                GameId = 1303,
                Version = "1.0.5",
            },
            new()
            {
                Name = "1.2-SpaceCore-1.1.0.zip",
                FileId = 7516,
                GameId = 1303,
                Version = "1.1.0",
            },
            new()
            {
                Name = "1.2-SpaceCore-1.1.1.zip",
                FileId = 7718,
                GameId = 1303,
                Version = "1.1.1",
            },
        },
    };
}
