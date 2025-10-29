using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using static NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles.Helpers;
namespace NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles;

public class MarkUsedLibraryFilesTest(IServiceProvider serviceProvider, ILibraryService libraryService) : AGameTest<StubbedGame>(serviceProvider)
{
    [Fact]
    public async Task ShouldMarkAndUnmarkFilesCorrectly()
    {
        // Arrange
        var loadout = await CreateLoadout();
        var modFiles = new List<RelativePath>
        {
            "file-0", "file-1", "file-2",
            "file-3", "file-4", "file-5",
        };

        // Add files to the loadout
        var tx = Connection.BeginTransaction();
        var libraryArchive = await CreateLibraryArchive(Connection, "FunNeverStopsAtTheNexus.zip");
        var (archiveLocation, hashes) = await AddModAsync(tx, modFiles, loadout, "Fun Never Stops at the Nexus", libraryArchive);
        await tx.Commit();

        // Refresh the loadout to get the updated state
        Refresh(ref loadout);
        
        // Act
        var gc = CreateGC(archiveLocation);

        // All files should be marked as used, and loadout items in use.
        for (var x = 0; x < modFiles.Count; x++)
            IsFileReferenced(gc, hashes[x]).Should().BeTrue($"File with hash {hashes[x]} should be marked as used");

        AssertAllLoadoutItemsAre(used: true, reason: "LoadoutFile with hash {0} should be marked as used", gc, loadout);

        // Delete the loadout
        // ReSharper disable once RedundantArgumentDefaultValue
        await DeleteLoadoutAsync(loadout.Id, GarbageCollectorRunMode.DoNotRun);
        
        // Do a GC run again, this time the loadout is deleted, however they still exist in library,
        // and thus should not be removed.
        gc = CreateGC(archiveLocation);
        AssertAllLoadoutItemsAre(used: true, reason: "The LibraryItem still exists; therefore, the. (File with Hash {} should still exist)", gc, loadout);
        
        // Now delete the library item, and hopefully GC will clean up the rest.
        var libraryArchives = new[] { libraryArchive.AsLibraryFile().AsLibraryItem() };
        // ReSharper disable once RedundantArgumentDefaultValue
        await libraryService.RemoveLibraryItems(libraryArchives, GarbageCollectorRunMode.DoNotRun);
        
        gc = CreateGC(archiveLocation);
        AssertAllLoadoutItemsAre(used: false, reason: "LoadoutFile with hash {0} should not be marked as used after Loadout AND Library deletion.", gc, loadout);
    }
    private ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> CreateGC(AbsolutePath archiveLocation)
    {
        ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> gc;
        gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        gc.AddArchive(archiveLocation, GetParsedNxHeader(archiveLocation));
        DataStoreReferenceMarker.MarkUsedFiles(Connection, gc);
        return gc;
    }

    private static void AssertAllLoadoutItemsAre(bool used, string reason, ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> gc, Loadout.ReadOnly loadout)
    {
        // Verify that all LoadoutFiles in the loadout are marked as used
        foreach (var item in loadout.Items)
        {
            var hasTargetPath = item.TryGetAsLoadoutItemWithTargetPath(out var targetPath);
            var hasLoadoutFile = targetPath.TryGetAsLoadoutFile(out var loadoutFile);
            if (!hasTargetPath || !hasLoadoutFile)
                continue;
            
            IsFileReferenced(gc, loadoutFile.Hash).Should().Be(used, reason, loadoutFile.Hash);
        }
    }

    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
