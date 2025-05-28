using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.CrossPlatform;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using Xunit.Abstractions;

namespace NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles;

/// <summary>
/// This ensures that 'game files' are marked as roots when performing the GC action.
/// That is, the backup of game data which we made.
/// </summary>
public class MarkGameTestFilesTest(ITestOutputHelper testOutputHelper) : AGCStubbedGameTest<MarkGameTestFilesTest>(testOutputHelper)
{
    [Fact]
    public async Task GameFilesAreRooted()
    {
        // Setup: Manage a game and make a 'vanilla' loadout.
        // This will run the synchronizer, and thus in turn ingest the tested file from GCStubbedGame
        await CreateLoadout();

        // As a sanity test, confirm that we have backed up the test file.
        // This proves our initial assertion that synchronizer runs as expected
        (await FileStore.HaveFile(ExpectedHash)).Should().Be(true);
        
        // Act: Run a GC.
        var gc = CreateGC();
        RunGarbageCollector(gc, out _);

        // Assert: No game files should be deleted from FileStore, they are roots.
        (await FileStore.HaveFile(ExpectedHash)).Should().Be(true);
        
        // Unmanage the game (with GC)
        await Synchronizer.UnManage(GameInstallation, runGc: true);
        
        // The file should be gone, because we deleted/retracted all the roots.
        (await FileStore.HaveFile(ExpectedHash)).Should().Be(false);
    }
    
    private AbsolutePath RunGarbageCollector(ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> collector, out List<Hash> toDelete)
    {
        AbsolutePath newArchivePath = default;
        List<Hash> toDel = null!;
        collector.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archive) =>
            {
                toDel = toRemove;
                NxRepacker.RepackArchive(progress, toArchive, toRemove, archive, true, out newArchivePath);
            }
        );

        toDelete = toDel;
        return newArchivePath;
    }
    
    private ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> CreateGC()
    {
        // Note: This ignores stubbed game files, we're not testing for those.
        var gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        DataStoreReferenceMarker.MarkUsedFiles(Connection, gc); // <= picks up our marker on GcRootFileName
        return gc;
    }
}

/// <summary>
/// A stubbed game for GC testing.
/// </summary>
public class AGCStubbedGameTest<TTest> : AIsolatedGameTest<TTest, StubbedGame>
{
    /// <summary/>
    public const string GcRootFileName = "FunIsInfinite.exe";
    
    /// <summary>
    /// The expected hash of the GC root file.
    /// </summary>
    public Hash ExpectedHash { get; set; } = default!;
    
    /// <inheritdoc />
    public AGCStubbedGameTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

    /// <summary/>
    /// <remarks>
    /// Our 'stubbed game' infrastructure is set up in such a way where
    /// we index and backup the game files on disk in this thing called the <see cref="StubbedFileHasherService"/>.
    ///
    /// So we begin in a state where we have the base files archived and not on disk.
    /// A Synchronize puts them on disk. Running the GC would wipe these archived files
    /// as they have no root markers on them. (Not gyuuud~, o nyoooo! 😿)
    /// [Unless we patch test bootstrap]
    ///
    /// However, in real life, the App doesn't work like that ‼️.
    /// The backed up files have to come from somewhere 💡.
    /// So actually, in practice, when managing a real game, we usually make the
    /// file backups of game files on first Synchronize. 🔥
    ///
    /// This is also true for when we have a mod, like SMAPI which currently (on Windows)
    /// replaces an existing game file (the EXE 🔥) with SMAPI itself.
    ///
    /// So at that point we need to mark files as roots (mhm mhm!!).
    /// In order to test this, we dump a file on disk, so it's seen as part of the
    /// game itself; thus we can test it! 😉💜
    /// </remarks>
    protected override async Task GenerateGameFiles()
    {
        var register = GameInstallation.LocationsRegister;
        var gameFolder = register.GetTopLevelLocations().First(x => x.Key == LocationId.Game);
        var destination = gameFolder.Value.Combine(GcRootFileName);

        FileSystem.CreateDirectory(gameFolder.Value);
        await using var file = FileSystem.CreateFile(destination);
        file.Write("hello"u8);
        file.Seek(0, SeekOrigin.Begin);

        // Get the hash of the item we expect after first synchronize.
        ExpectedHash = await file.HashingCopyAsync(Stream.Null, CancellationToken.None);
    }
    
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStandardGameLocators(false)
            .AddStubbedGameLocators();
    }
}
