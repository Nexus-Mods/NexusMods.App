using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates;
using NexusMods.Paths;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.TestFramework;
using Xunit.Abstractions;

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
public class ModUpdateServiceTests : ACyberpunkIsolatedGameTest<ModUpdateServiceTests> // game doesn't matter here, we just don't have SDV.
{
    private readonly ILibraryService _libraryService;
    private readonly IModUpdateService _modUpdateService;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly FakeTimeProvider _timeProvider;

    public ModUpdateServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _temporaryFileManager = ServiceProvider.GetRequiredService<TemporaryFileManager>();
        _libraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        _nexusModsLibrary = ServiceProvider.GetRequiredService<NexusModsLibrary>();
        _timeProvider = new FakeTimeProvider();
        _modUpdateService = new ModUpdateService(
            ServiceProvider.GetRequiredService<IConnection>(),
            ServiceProvider.GetRequiredService<INexusApiClient>(),
            ServiceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>(),
            ServiceProvider.GetRequiredService<ILogger<ModUpdateService>>(),
            ServiceProvider.GetRequiredService<NexusGraphQLClient>(),
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
    public async Task GetNewestFileVersionObservable_ShouldNotifyOnLibraryItemAdd()
    {
        // This tests that our real time update firing mechanism works.
        // As a mod is added to library, the 'observable' should fire immediately.
        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;

        // Download an old version of SpaceCore
        await using var tempFile = _temporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );
    
        // Setup our listening.
        var observable = _modUpdateService.GetNewestFileVersionObservable(downloadJob.Job.FileMetadata);
    
        // Create collection for results
        var results = new List<ModUpdateOnPage>();
        using var subscription = observable.Subscribe(val => results.Add(val.Value));
    
        // Right into the library.
        await _libraryService.AddDownload(downloadJob);

        // Assert
        // At least this amount of updates
        results.Should().HaveCount(1);
        var updateOnPage = results[0];
        updateOnPage.NewerFiles.Should().HaveCountGreaterThanOrEqualTo(spaceCoreData.Updates.Length);

        // And check all expected updates were found.
        AssertUpdatesContainAllResults(spaceCoreData.Updates, updateOnPage);
    }
    
    [Fact]
    public async Task GetNewestFileVersionObservable_ShouldNotifyOnLibraryItemRemove()
    {
        // This tests that our real time update firing mechanism works.
        // As a mod is removed from library, the 'observable' should emit a 'null'
        // object to indicate there is not an update anymore.

        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;

        // Download an old version of SpaceCore
        await using var tempFile = _temporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );
    
        // Setup our listening.
        var observable = _modUpdateService.GetNewestFileVersionObservable(downloadJob.Job.FileMetadata);
    
        // Create collection for results
        var receivedRemove = false;
        using var subscription = observable.Subscribe(val =>
            {
                if (!val.HasValue)
                    receivedRemove = true;
            }
        );
    
        // Right into the library.
        // This fires the event.
        var libraryFile = await _libraryService.AddDownload(downloadJob);
        receivedRemove.Should().BeFalse();

        // Now remove it from the library.
        await _libraryService.RemoveItems([libraryFile.AsLibraryItem()], GarbageCollectorRunMode.DoNotRun);
        receivedRemove.Should().BeTrue();
    }

    [Fact]
    public async Task GetNewestModPageVersionObservable_ShouldNotifyOnLibraryItemAdd()
    {
        // This tests that our real time update firing mechanism works.
        // As a mod is added to library, the 'observable' for mod page should fire immediately.

        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;

        // Download an old version of SpaceCore
        await using var tempFile = _temporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );

        // Get the mod page metadata
        var modPageMetadata = downloadJob.Job.FileMetadata.ModPage;

        // Setup our listening for mod page updates
        var observable = _modUpdateService.GetNewestModPageVersionObservable(modPageMetadata);

        // Create collection for results
        var results = new List<ModUpdatesOnModPage>();
        using var subscription = observable.Subscribe(val => results.Add(val.Value));

        // Right into the library.
        await _libraryService.AddDownload(downloadJob);

        // Assert
        // At least this amount of updates
        results.Should().HaveCount(1);
        var updatesOnModPage = results[0];
        var newestFile = updatesOnModPage.NewestFile();

        // Check the returned file is 'sensible'
        newestFile.Uid.GameId.Should().Be((GameId)spaceCoreData.GameId);
        newestFile.ModPage.Uid.ModId.Should().Be((ModId)spaceCoreData.ModId);
        
        // And check for all expected update files.
        AssertUpdatesContainAllResults(spaceCoreData.Updates, updatesOnModPage.FileMappings[0]);
    }

    [Fact]
    public async Task GetNewestModPageVersionObservable_ShouldNotifyOnLibraryItemRemove()
    {
        // This tests that our real time update firing mechanism works.
        // As a mod is removed from library, the 'observable' for mod page
        // should emit a 'null' object to indicate there is not an update anymore.
        
        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;

        // Download an old version of SpaceCore
        await using var tempFile = _temporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );

        // Get the mod page metadata
        var modPageMetadata = downloadJob.Job.FileMetadata.ModPage;

        // Setup our listening for mod page updates
        var observable = _modUpdateService.GetNewestModPageVersionObservable(modPageMetadata);

        // Create collection for results
        var receivedRemove = false;
        using var subscription = observable.Subscribe(val =>
            {
                if (!val.HasValue)
                    receivedRemove = true;
            }
        );

        // Right into the library.
        // This fires the event.
        var libraryFile = await _libraryService.AddDownload(downloadJob);
        receivedRemove.Should().BeFalse();

        // Now remove it from the library.
        await _libraryService.RemoveItems([libraryFile.AsLibraryItem()], GarbageCollectorRunMode.DoNotRun);
        receivedRemove.Should().BeTrue();
    }

    [Fact]
    public async Task GetNewestFileVersionObservable_ShouldNotRemoveUnrelatedModUpdates_WhenModOnAnotherPageIsUpdated()
    {
        // This tests the 'fast path' of the real time update mechanism.
        // When receiving mod updates via the library, we only update a small subset
        // of the underlying ObservableCache. This test ensures that an update in
        // one mod does not emit a removal of an update in another, which could
        // otherwise happen due to incorrect logic.

        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;
        var smapiData = StaticTestData.SmapiModData;

        // Download an old version of SpaceCore
        await using var tempSpaceCoreFile = _temporaryFileManager.CreateFile();
        var spaceCoreDownloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempSpaceCoreFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );

        // Download an old version of SMAPI
        await using var tempSmapiFile = _temporaryFileManager.CreateFile();
        var smapiDownloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempSmapiFile,
            gameId: (GameId)smapiData.GameId,
            modId: (ModId)smapiData.ModId,
            fileId: (FileId)smapiData.FileId
        );

        // Setup our listening for SMAPI updates
        var smapiObservable = _modUpdateService.GetNewestFileVersionObservable(smapiDownloadJob.Job.FileMetadata);

        // Add both mods to the library
        _ = await _libraryService.AddDownload(spaceCoreDownloadJob);
        _ = await _libraryService.AddDownload(smapiDownloadJob);
        
        // Track if we receive a "remove" notification for SMAPI
        var receivedRemove = false;
        using var smapiSubscription = smapiObservable.Subscribe(val =>
            {
                if (!val.HasValue)
                    receivedRemove = true;
            }
        );
        
        // Now update SpaceCore to a newer version
        var spaceCoreUpdate = spaceCoreData.Updates[0]; // Select the first update (1.0.2), hardcoded for easier debug.
        await using var tempSpaceCoreUpdateFile = _temporaryFileManager.CreateFile();
        var spaceCoreUpdateDownloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempSpaceCoreUpdateFile,
            gameId: (GameId)spaceCoreUpdate.GameId,
            modId: (ModId)spaceCoreUpdate.ModId,
            fileId: (FileId)spaceCoreUpdate.FileId
        );
        
        // Add the SpaceCore update to the library
        await _libraryService.AddDownload(spaceCoreUpdateDownloadJob);

        // Assert that SMAPI was not affected by the SpaceCore update
        receivedRemove.Should().BeFalse("SMAPI updates should not be removed when SpaceCore is updated");
    }
    
    [Fact]
    public async Task GetNewestFileVersionObservable_ShouldNotifyOlderVersionWhenCurrentVersionRemoved()
    {
        // This tests that when both an older and current version of a mod are installed,
        // and the current version is removed, the older version should receive update notification
        // instantly.
        
        // Arrange
        var spaceCoreData = StaticTestData.SpaceCoreModData;
        var spaceCoreUpdate = spaceCoreData.Updates[^1]; // Get the latest update (1.1.1)

        // Download an old version of SpaceCore
        await using var tempOldFile = _temporaryFileManager.CreateFile();
        var oldVersionDownloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempOldFile,
            gameId: (GameId)spaceCoreData.GameId,
            modId: (ModId)spaceCoreData.ModId,
            fileId: (FileId)spaceCoreData.FileId
        );
        
        // Download a newer version of SpaceCore (known).
        await using var tempCurrentFile = _temporaryFileManager.CreateFile();
        var currentVersionDownloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempCurrentFile,
            gameId: (GameId)spaceCoreUpdate.GameId,
            modId: (ModId)spaceCoreUpdate.ModId,
            fileId: (FileId)spaceCoreUpdate.FileId
        );
        
        // Setup our listening for the old version updates
        var oldVersionObservable = _modUpdateService.GetNewestFileVersionObservable(oldVersionDownloadJob.Job.FileMetadata);
        
        // Create collection for results
        var updateResults = new List<ModUpdateOnPage>();
        using var subscription = oldVersionObservable.Subscribe(val => 
        {
            if (val.HasValue)
                updateResults.Add(val.Value);
            else
                updateResults.Clear();
        });
        
        // Add both versions to the library
        await _libraryService.AddDownload(oldVersionDownloadJob);
        updateResults.Should().NotBeEmpty("Adding the old version should add an update.");
        var currentLibraryFile = await _libraryService.AddDownload(currentVersionDownloadJob);
        updateResults.Should().BeEmpty("Installing latest version should remove the update.");
        
        // Now remove the current version from the library
        await _libraryService.RemoveItems([currentLibraryFile.AsLibraryItem()], GarbageCollectorRunMode.DoNotRun);
        
        // Assert that the old version now has update notifications
        updateResults.Should().NotBeEmpty("Old version should receive updates after newer version is removed");
        
        // And check all expected updates were found in the results
        var updateOnPage = updateResults[0];
        AssertUpdatesContainAllResults(spaceCoreData.Updates, updateOnPage);
    }
    
    private static void AssertUpdatesContainAllResults(StaticTestData.TestModData[] updates, ModUpdateOnPage modUpdate)
    {
        foreach (var expectedUpdate in updates)
        {
            // Find the fle by update ID
            var found = modUpdate.NewerFiles.FirstOrDefault(x => x.Uid.FileId == expectedUpdate.FileId);
            
            // Did we find it?
            found.Should().NotBeNull();
            
            // Check the details match.
            expectedUpdate.Version.Should().Be(found.Version);
            expectedUpdate.Name.Should().Be(found.Name);
            expectedUpdate.ModId.Should().Be(found.ModPage.Uid.ModId.Value);
            expectedUpdate.GameId.Should().Be(found.Uid.GameId.Value);
        }
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
        /// 32-bit ID of mod on NexusMods.com
        /// </summary>
        public uint ModId { get; init; }

        /// <summary>
        /// Version as attached to the file to Nexus.
        /// </summary>
        public string Version { get;init; }

        /// <summary>
        /// Expected updates for this file.
        /// These are not all the updates, only most of them.
        /// </summary>
        public TestModData[] Updates { get; init; }
    }

    public static readonly TestModData SmapiModData = new()
    {
        Name = "SMAPI 4.0.8",
        FileId = 94412,
        GameId = 1303,
        Version = "4.0.8",
        ModId = 2400,
        Updates =
        [
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.1",
                FileId = 109556,
                GameId = 1303,
                Version = "4.1.0-beta.1",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.2",
                FileId = 110769,
                GameId = 1303,
                Version = "4.1.0-beta.2",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.3",
                FileId = 112400,
                GameId = 1303,
                Version = "4.1.0-beta.3",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.4",
                FileId = 112827,
                GameId = 1303,
                Version = "4.1.0-beta.4",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0-beta.5",
                FileId = 113649,
                GameId = 1303,
                Version = "4.1.0-beta.5",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.0",
                FileId = 115051,
                GameId = 1303,
                Version = "4.1.0",
                ModId = 2400,
            },
            new TestModData
            {
                Name = "SMAPI 4.1.1",
                FileId = 115121,
                GameId = 1303,
                Version = "4.1.1",
                ModId = 2400,
            },
        ],
    };


    public static readonly TestModData SpaceCoreModData = new()
    {
        Name = "1.2-SpaceCore-1.0.1.zip",
        FileId = 5001,
        GameId = 1303,
        Version = "1.0.1",
        ModId = 1348,
        Updates =
        [
            new TestModData
            {
                Name = "1.2-SpaceCore-1.0.2.zip",
                FileId = 5018,
                GameId = 1303,
                Version = "1.0.2",
                ModId = 1348,
            },
            new TestModData
            {
                Name = "1.2-SpaceCore-1.0.3.zip",
                FileId = 5071,
                GameId = 1303,
                Version = "1.0.3",
                ModId = 1348,
            },
            new TestModData
            {
                Name = "1.2-SpaceCore-1.0.4.zip",
                FileId = 6066,
                GameId = 1303,
                Version = "1.0.4",
                ModId = 1348,
            },
            new TestModData
            {
                Name = "1.2-SpaceCore-1.0.5.zip",
                FileId = 6077,
                GameId = 1303,
                Version = "1.0.5",
                ModId = 1348,
            },
            new TestModData
            {
                Name = "1.2-SpaceCore-1.1.0.zip",
                FileId = 7516,
                GameId = 1303,
                Version = "1.1.0",
                ModId = 1348,
            },
            new TestModData
            {
                Name = "1.2-SpaceCore-1.1.1.zip",
                FileId = 7718,
                GameId = 1303,
                Version = "1.1.1",
                ModId = 1348,
            },
        ],
    };
}
