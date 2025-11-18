using System.Text;

using NexusMods.Games.TestFramework;
using NexusMods.Sdk.Games;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralFileManagementTests (ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{
    [Fact]
    public async Task SynchronizerFileManagementTest()
    {
        var sb = new StringBuilder();
        
        var loadoutA = await CreateLoadout();
        loadoutA = await Synchronizer.Synchronize(loadoutA);

        LogDiskState(sb, "## 1 - Loadout Created (A) - Synced",
            """
            Added a new loadout and synced it.
            """, [loadoutA]);
        
        // Add a new file to the game
        var newfileGamePath = new GamePath(LocationId.Game, "bin/newFile.txt");
        var newFileFullPath = GameInstallation.Locations.ToAbsolutePath(newfileGamePath);
        newFileFullPath.Parent.CreateDirectory();
        await newFileFullPath.WriteAllTextAsync("Hello World!");
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);

        LogDiskState(sb, "## 2 - Added bin/newFile - Synced",
            """
            Added a new file to the game and synced it.
            """, [loadoutA]);
        
        // Update the new file contents
        await newFileFullPath.WriteAllTextAsync("Hello World! Updated!");
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);

        LogDiskState(sb, "## 2 - Updated the file - Synced",
            """
            Updated the new file and synced it.
            """, [loadoutA]);
        
        await Verify(sb.ToString(), extension: "md");
    }
}
