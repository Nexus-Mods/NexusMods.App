using System.Text;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.TestFramework;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralFileManagementTests (ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{
    [Fact]
    public async Task SynchronizerFileManagementTest()
    {
        var sb = new StringBuilder();
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        var loadoutA = await CreateLoadout(false);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanGameFiles(GameInstallation);
        
        LogDiskState(sb, "## 1 - Loadout Created (A) - Synced",
            """
            Added a new loadout and synced it.
            """, [loadoutA]);
        
        // Add a new file to the game
        var newfileGamePath = new GamePath(LocationId.Game, "bin/newFile.txt");
        var newFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(newfileGamePath);
        newFileFullPath.Parent.CreateDirectory();
        await newFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanGameFiles(GameInstallation);
        
        LogDiskState(sb, "## 2 - Added bin/newFile - Synced",
            """
            Added a new file to the game and synced it.
            """, [loadoutA]);
        
        // Update the new file contents
        await newFileFullPath.WriteAllTextAsync("Hello World! Updated!");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanGameFiles(GameInstallation);
        
        LogDiskState(sb, "## 2 - Updated the file - Synced",
            """
            Updated the new file file and synced it.
            """, [loadoutA]);
        
        await Verify(sb.ToString(), extension: "md");
    }
}
