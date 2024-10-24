using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.DataModel;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class FullSystemTest(IServiceProvider serviceProvider, ISettingsManager settingsManager) : AGameTest<StubbedGame>(serviceProvider)
{
    private readonly NxFileStore _fileStore = serviceProvider.GetRequiredService<NxFileStore>();
    private readonly DataModelSettings _dataModelSettings = settingsManager.Get<DataModelSettings>();

    [Fact]
    public async Task FullGarbageCollectionProcess_ShouldRemoveUnusedFiles()
    {
        // Arrange
        var loadout1 = await CreateLoadout();
        var loadout2 = await CreateLoadout();

        var sharedFiles = new List<RelativePath> { "shared1.txt", "shared2.txt" };
        var loadout1Files = new List<RelativePath> { "loadout1_1.txt", "loadout1_2.txt" };
        var loadout2Files = new List<RelativePath> { "loadout2_1.txt", "loadout2_2.txt" };

        List<Hash> loadout1SharedModHashes;
        List<Hash> loadout2SharedModHashes;
        List<Hash> loadout1ModHashes;
        List<Hash> loadout2ModHashes;

        using (var tx = Connection.BeginTransaction())
        {
            // Add shared files to both loadouts
            (_, loadout1SharedModHashes) = await AddModAsync(tx, sharedFiles, loadout1, "SharedMod");
            (_, loadout2SharedModHashes) = await AddModAsync(tx, sharedFiles, loadout2, "SharedMod");

            // Add specific files to each loadout
            (_, loadout1ModHashes) = await AddModAsync(tx, loadout1Files, loadout1, "Loadout1Mod");
            (_, loadout2ModHashes) = await AddModAsync(tx, loadout2Files, loadout2, "Loadout2Mod");
            await tx.Commit();
        }

        Refresh(ref loadout1);
        Refresh(ref loadout2);

        // Act: All files are referenced.
        RunGarbageCollector.Do(_dataModelSettings.ArchiveLocations, _fileStore, Connection);

        // Assert that all files exist after a GC where everything is used.
        foreach (var hash in loadout2ModHashes.Concat(loadout1ModHashes)
                     .Concat(loadout1SharedModHashes)
                     .Concat(loadout2SharedModHashes))
        {
            (await _fileStore.HaveFile(hash)).Should().BeTrue();
        }

        // Act: All files are referenced.
        await DeleteLoadoutAsync(loadout2, GarbageCollectorRunMode.RunSynchronously);
        
        // Assert that all files except for loadout2ModHashes (deleted via loadout delete).
        foreach (var hash in loadout1ModHashes.Concat(loadout1SharedModHashes)
                     .Concat(loadout2SharedModHashes))
        {
            (await _fileStore.HaveFile(hash)).Should().BeTrue();
        }
        
        // And assert loadout2ModHashes were removed.
        foreach (var hash in loadout2ModHashes)
        {
            (await _fileStore.HaveFile(hash)).Should().BeFalse();
        }
    }
    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
