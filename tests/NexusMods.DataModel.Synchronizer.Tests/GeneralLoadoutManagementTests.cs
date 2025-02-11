using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralLoadoutManagementTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralLoadoutManagementTests>(helper)
{

    [Fact]
    public async Task SynchronizerIntegrationTests()
    {
        var sb = new StringBuilder();
        
        var originalFileGamePath = new GamePath(LocationId.Game, "bin/originalGameFile.txt");
        var originalFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(originalFileGamePath);
        originalFileFullPath.Parent.CreateDirectory();
        await originalFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 1 - Initial State",
            """
            The initial state of the game folder should contain the game files as they were created by the game store. No loadout has been created yet.
            """);
        var loadoutA = await CreateLoadout();

        LogDiskState(sb, "## 2 - Loadout Created (A) - Synced",
            """
            A new loadout has been created and has been synchronized, so the 'Last Synced State' should be set to match the new loadout.
            """, [loadoutA]);

        var newFileInGameFolderA = new GamePath(LocationId.Game, "bin/newFileInGameFolderA.txt");
        var newFileFullPathA = GameInstallation.LocationsRegister.GetResolvedPath(newFileInGameFolderA);
        newFileFullPathA.Parent.CreateDirectory();
        await newFileFullPathA.WriteAllTextAsync("New File for this loadout");
        
        await Synchronizer.RescanFiles(GameInstallation);
        LogDiskState(sb, "## 4 - New File Added to Game Folder",
            """
            New files have been added to the game folder by the user or the game, but the loadout hasn't been synchronized yet.
            """, [loadoutA]);

        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 5 - Loadout Synced with New File",
            """
            After the loadout has been synchronized, the new file should be added to the loadout.
            """, [loadoutA]);
        
        
        await Synchronizer.DeactivateCurrentLoadout(GameInstallation);
        LogDiskState(sb, "## 6 - Loadout Deactivated", 
            """
            At this point the loadout is deactivated, and all the files in the current state should match the initial state.
            """, [loadoutA]);
        
        
        var loadoutB = await CreateLoadout();

        LogDiskState(sb, "## 7 - New Loadout (B) Created - No Sync",
            """
            A new loadout is created, but it has not been synchronized yet. So again the 'Last Synced State' is not set.
            """, [loadoutA, loadoutB]
        );
        
        loadoutB = await Synchronizer.Synchronize(loadoutB);
        
        LogDiskState(sb, "## 8 - New Loadout (B) Synced",
            """
            After the new loadout has been synchronized, the 'Last Synced State' should match the 'Current State' as the loadout has been applied to the game folder. Note that the contents of this 
            loadout are different from the previous loadout due to the new file only being in the previous loadout.
            """, [loadoutA, loadoutB]
        );
        
        var newFileInGameFolderB = new GamePath(LocationId.Game, "bin/newFileInGameFolderB.txt");
        var newFileFullPathB = GameInstallation.LocationsRegister.GetResolvedPath(newFileInGameFolderB);
        newFileFullPathB.Parent.CreateDirectory();
        await newFileFullPathB.WriteAllTextAsync("New File for this loadout, B");
        
        loadoutB = await Synchronizer.Synchronize(loadoutB);
        
        LogDiskState(sb, "## 9 - New File Added to Game Folder (B)",
            """
            A new file has been added to the game folder and B loadout has been synchronized. The new file should be added to the B loadout.
            """, [loadoutA, loadoutB]
        );
        

        await Synchronizer.DeactivateCurrentLoadout(GameInstallation);
        await Synchronizer.ActivateLoadout(loadoutA);
        
        LogDiskState(sb, "## 10 - Switch back to Loadout A",
            """
            Now we switch back to the A loadout, and the new file should be removed from the game folder.
            """, [loadoutA, loadoutB]
        );
        
        var loadoutC = await Synchronizer.CopyLoadout(loadoutA);
        
        LogDiskState(sb, "## 11 - Loadout A Copied to Loadout C",
            """
            Loadout A has been copied to Loadout C, and the contents should match.
            """, [loadoutA, loadoutB, loadoutC]
        );
        
        await Synchronizer.UnManage(GameInstallation);
        
        LogDiskState(sb, "## 12 - Game Unmanaged",
            """
            The loadouts have been deleted and the game folder should be back to its initial state.
            """,
        [loadoutA.Rebase(), loadoutB.Rebase()]);
        
        await Verify(sb.ToString(), extension: "md");
    }

    [Fact]
    public async Task SwappingLoadoutsDoesNotLeakFiles()
    {
        var sb = new StringBuilder();
        var loadoutA = await CreateLoadout();
        var loadoutB = await CreateLoadout();
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 1 - Loadout A Synced",
            """
            Loadout A has been synchronized, and the game folder should match the loadout.
            """, [loadoutA, loadoutB]);
        
        var newFileInGameFolderA = new GamePath(LocationId.Game, "bin/newFileInGameFolderA.txt");
        var newFileFullPathA = GameInstallation.LocationsRegister.GetResolvedPath(newFileInGameFolderA);
        newFileFullPathA.Parent.CreateDirectory();
        await newFileFullPathA.WriteAllTextAsync("New File for this loadout");

        await Synchronizer.RescanFiles(loadoutA.InstallationInstance);
        
        LogDiskState(sb, "## 2 - New File Added to Game Folder",
            """
            A new file has been added to the game folder, and the loadout has been synchronized. The new file should be added to the loadout.
            """, [loadoutA, loadoutB]);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 3 - Loadout A Synced with New File",
            """
            Loadout A has been synchronized again, and the new file should be added to the disk state.
            """, [loadoutA, loadoutB]);
        
        loadoutB = await Synchronizer.Synchronize(loadoutB);
        
        LogDiskState(sb, "## 4 - Loadout B Synced",
            """
            Loadout B has been synchronized, the added file should be removed from the disk state, and only exist in loadout A.
            """, [loadoutA, loadoutB]);
        
        
        var tree = await Synchronizer.BuildSyncTree(loadoutA);
        Synchronizer.ProcessSyncTree(tree);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 5 - Loadout A Synced Again",
            """
            Loadout A has been synchronized again, and the new file should be added to the disk state.
            """, [loadoutA, loadoutB]);
        
        await Verify(sb.ToString(), extension: "md");
    }

    [Fact]
    public async Task DeletedFilesStayDeletedWhenModIsReenabled()
    {
        var sb = new StringBuilder();
        var loadoutA = await CreateLoadout();
        var loadoutB = await CreateLoadout();
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 1 - Loadout A Synced",
            """
            Loadout A has been synchronized, and the game folder should match the loadout.
            """, [loadoutA, loadoutB]);
        
        var modFile = FileSystem.GetKnownPath(KnownPath.EntryDirectory) / "Resources" / "TestMod.zip";
        var libraryFile = await LibraryService.AddLocalFile(modFile);
        var mod = await LibraryService.InstallItem(libraryFile.AsLibraryFile().AsLibraryItem(), loadoutA);
        loadoutA = loadoutA.Rebase();
        
        LogDiskState(sb, "## 2 - Loadout A Mod Added",
            """
            A mod has been added but not yet synced, so only the loadout has the file.
            """, [loadoutA, loadoutB]);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 3 - Loadout A Synced",
            """
            Loadout A has been synchronized, and the game folder should match the loadout.
            """, [loadoutA, loadoutB]);


        
        var testFilePath = new GamePath(LocationId.Game, "bin/x64/ThisIsATestFile.txt");
        var otherTestFilePath = new GamePath(LocationId.Game, "bin/x64/And Another One.txt");
        
        var diskPath = loadoutA.InstallationInstance.LocationsRegister.GetResolvedPath(testFilePath);
        var otherDiskPath = loadoutA.InstallationInstance.LocationsRegister.GetResolvedPath(otherTestFilePath);
        diskPath.Delete();
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 4 - Deleted file from disk",
            """
            A mod file has been deleted from disk, so that information should be synced to the loadout.
            """, [loadoutA, loadoutB]);
        

        using var tx = Connection.BeginTransaction();
        tx.Add(mod, LoadoutItem.Disabled, Null.Instance);
        await tx.Commit();
        
        loadoutA = loadoutA.Rebase();

        LogDiskState(sb, "## 5 - Disabled the mod group",
            """
            The mod has been disabled, but not yet synched
            """, [loadoutA, loadoutB]);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 6 - Loadout A Synced",
            """
            Loadout A has been synchronized, the mod files shouldn't show back up.
            """, [loadoutA, loadoutB]);
        
        // Re-enable the mod
        using var tx2 = Connection.BeginTransaction();
        tx2.Retract(mod, LoadoutItem.Disabled, Null.Instance);
        await tx2.Commit();
        
        loadoutA = loadoutA.Rebase();
        
        LogDiskState(sb, "## 6 - Enable the mod group",
            """
            Re-enable the mod.
            """, [loadoutA, loadoutB]);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 7 - Loadout A Synced",
            """
            Re-enable the mod.
            """, [loadoutA, loadoutB]);
        
        diskPath.FileExists.Should().BeFalse("The file should still be deleted");
        
        loadoutB = await Synchronizer.Synchronize(loadoutB);
        
        LogDiskState(sb, "## 8 - Loadout B Synced",
            """
            Loadout B has been synchronized, the file should still be deleted as well as the other mod file.
            """, [loadoutA, loadoutB]);
        
        diskPath.FileExists.Should().BeFalse("The file should still be deleted");
        otherDiskPath.FileExists.Should().BeFalse("The other file is in a mod not in this loadout");
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogDiskState(sb, "## 9 - Loadout A Synced",
            """
            Loadout A has been synchronized, the file should still be deleted but the other file in the mod should be back.
            """, [loadoutA, loadoutB]);
        
        diskPath.FileExists.Should().BeFalse("The file should still be deleted");
        otherDiskPath.FileExists.Should().BeTrue("This file is not deleted and is in a mod in this loadout");
        
        await Verify(sb.ToString(), extension: "md");
        
    }

}
