using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using static NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles.Helpers;
namespace NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles;

public class MarkUsedLoadoutFilesTest(IServiceProvider serviceProvider) : AGameTest<StubbedGame>(serviceProvider)
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
        var (archiveLocation, hashes) = await AddModAsync(tx, modFiles, loadout, "Infinite Worlds Collide at the Nexus");
        await tx.Commit();

        // Refresh the loadout to get the updated state
        Refresh(ref loadout);
        
        // Act
        var gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        gc.AddArchive(archiveLocation, GetParsedNxHeader(archiveLocation));
        DataStoreReferenceMarker.MarkUsedFiles(Connection, gc);

        // Loadout is not removed, all files should be marked as used
        for (var x = 0; x < modFiles.Count; x++)
            IsFileReferenced(gc, hashes[x]).Should().BeTrue($"File with hash {hashes[x]} should be marked as used");

        // Verify that all LoadoutFiles in the loadout are marked as used
        foreach (var item in loadout.Items)
        {
            var hasTargetPath = item.TryGetAsLoadoutItemWithTargetPath(out var targetPath);
            var hasLoadoutFile = targetPath.TryGetAsLoadoutFile(out var loadoutFile);
            if (!hasTargetPath || !hasLoadoutFile)
                continue;
            
            IsFileReferenced(gc, loadoutFile.Hash).Should().BeTrue($"LoadoutFile with hash {loadoutFile.Hash} should be marked as used");
        }

        // Delete the loadout
        // ReSharper disable once RedundantArgumentDefaultValue
        await DeleteLoadoutAsync(loadout.Id, GarbageCollectorRunMode.DoNotRun);

        // Mark used files again
        gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        gc.AddArchive(archiveLocation, GetParsedNxHeader(archiveLocation));
        DataStoreReferenceMarker.MarkUsedFiles(Connection, gc);

        // Assert that no files are marked as used
        foreach (var item in loadout.Items)
        {
            var hasTargetPath = item.TryGetAsLoadoutItemWithTargetPath(out var targetPath);
            var hasLoadoutFile = targetPath.TryGetAsLoadoutFile(out var loadoutFile);
            if (!hasTargetPath || !hasLoadoutFile)
                continue;
            
            IsFileReferenced(gc, loadoutFile.Hash).Should().BeFalse($"LoadoutFile with hash {loadoutFile.Hash} should not be marked as used after deletion.");
        }
    }
    
    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
