using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralFileManagementTests (ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{
    [Fact]
    public async Task SynchronizerFileManagementTest()
    {
        var sb = new StringBuilder();
        
        await Synchronizer.RescanFiles(GameInstallation);
        var loadoutA = await CreateLoadout();
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 1 - Loadout Created (A) - Synced",
            """
            Added a new loadout and synced it.
            """, [loadoutA]);
        
        // Add a new file to the game
        var newfileGamePath = new GamePath(LocationId.Game, "bin/newFile.txt");
        var newFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(newfileGamePath);
        newFileFullPath.Parent.CreateDirectory();
        await newFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanFiles(GameInstallation);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 2 - Added bin/newFile - Synced",
            """
            Added a new file to the game and synced it.
            """, [loadoutA]);
        
        // Update the new file contents
        await newFileFullPath.WriteAllTextAsync("Hello World! Updated!");
        
        await Synchronizer.RescanFiles(GameInstallation);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 2 - Updated the file - Synced",
            """
            Updated the new file and synced it.
            """, [loadoutA]);
        
        await Verify(sb.ToString(), extension: "md");
    }

    [Fact]
    public async Task Test_FileConflicts()
    {
        var loadout = await CreateLoadout();

        Synchronizer.GetFileConflicts(Loadout.Load(Connection.Db, loadout)).Should().BeEmpty(because: "loadout is empty");
        Synchronizer.GetFileConflictsByParentGroup(Loadout.Load(Connection.Db, loadout)).Should().BeEmpty(because: "loadout is empty");

        var dataPath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources/TestMod.zip");
        var libraryArchive = await RegisterLocalArchive(dataPath);

        var collection1 = await CreateCollection(loadout, name: "Collection 1");
        var item1Result = await LibraryService.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout, parent: collection1.AsLoadoutItemGroup().LoadoutItemGroupId);
        var item1Id = item1Result.LoadoutItemGroup!.Value.Id;

        Synchronizer.GetFileConflicts(Loadout.Load(Connection.Db, loadout)).Should().BeEmpty(because: "no conflicts yet");
        Synchronizer.GetFileConflictsByParentGroup(Loadout.Load(Connection.Db, loadout)).Should().BeEmpty(because: "no conflicts yet");

        var collection2 = await CreateCollection(loadout, name: "Collection 2");
        var item2Result = await LibraryService.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout, parent: collection2.AsLoadoutItemGroup().LoadoutItemGroupId);
        var item2Id = item2Result.LoadoutItemGroup!.Value.Id;

        Synchronizer.GetFileConflicts(Loadout.Load(Connection.Db, loadout), removeDuplicates: true).Should().BeEmpty(because: "all conflicts are duplicates");
        Synchronizer.GetFileConflictsByParentGroup(Loadout.Load(Connection.Db, loadout), removeDuplicates: true).Should().BeEmpty(because: "all conflicts are duplicates");

        Synchronizer
            .GetFileConflicts(Loadout.Load(Connection.Db, loadout), removeDuplicates: false)
            .Should().HaveCount(2, because: "loadout has two file conflicts")
            .And.ContainKey(new GamePath(LocationId.Game, "bin/x64/ThisIsATestFile.txt"), because: "one of the conflicting files")
            .And.ContainKey(new GamePath(LocationId.Game, "bin/x64/And Another One.txt"), because: "one of the conflicting files")
            .And.AllSatisfy(kv => kv.Value.Items.Should()
                .AllSatisfy(value => kv.Key
                    .Equals(value.File.AsT0.AsLoadoutItemWithTargetPath().TargetPath).Should().BeTrue(because: "file should be grouped correctly")
                )
            );

        Synchronizer.GetFileConflictsByParentGroup(Loadout.Load(Connection.Db, loadout), removeDuplicates: false)
            .Should().HaveCount(2, because: "two groups have conflicts")
            .And.ContainKey(LoadoutItemGroup.Load(Connection.Db, item1Id))
            .And.ContainKey(LoadoutItemGroup.Load(Connection.Db, item2Id))
            .And.AllSatisfy(kv => kv.Value.Should().HaveCount(2, because: "group has two file conflicts"));
    }
}
