using System.Text;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralModManagementTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{

    [Fact]
    public async Task SynchronizerAddAndDisableMods()
    {
        var sb = new StringBuilder();
        
        var originalFileGamePath = new GamePath(LocationId.Game, "bin/originalGameFile.txt");
        var originalFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(originalFileGamePath);
        originalFileFullPath.Parent.CreateDirectory();
        await originalFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 1 - Initial State",
            """
            The initial state of the game, no loadout has been created yet.
            """);
        var loadoutA = await CreateLoadout();

        LogDiskState(sb, "## 2 - Loadout Created (A) - Synced",
            """
            Added a new loadout and synced it.
            """, [loadoutA]);
        
        var modAFiles = new List<RelativePath> { "bin/mods/modA/textureA.txt", "bin/mods/modA/meshA.txt", "bin/mods/shared/shared.txt" };
        var modBFiles = new List<RelativePath> { "bin/mods/modB/textureB.txt", "bin/mods/modB/meshB.txt", "bin/mods/shared/shared.txt" };
        
        
        // Add mod A to loadout A
        using (var tx = Connection.BeginTransaction())
        {
            await AddModAsync(tx, modAFiles,loadoutA, "ModA");
            await tx.Commit();
        }
        Refresh(ref loadoutA);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 3 - Added ModA to Loadout(A) - Synced",
            """
            Added ModA to Loadout A and synced it.
            """, [loadoutA]);
        
        
        // Add mod B to loadout A
        using (var tx = Connection.BeginTransaction())
        {
            await AddModAsync(tx, modBFiles, loadoutA, "ModB");
            await tx.Commit();
        }
        Refresh(ref loadoutA);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        
        LogDiskState(sb, "## 4 - Added ModB to Loadout(A) - Synced",
            """
            Added ModB to Loadout A and synced it.
            """, [loadoutA]);
     
 
        // Disable Mod B
        using (var tx = Connection.BeginTransaction())
        {
            var modB = LoadoutItem.FindByLoadout(Connection.Db, loadoutA).FirstOrOptional(li => li.Name == "ModB").Value;
            tx.Add(modB.Id, LoadoutItem.Disabled, Null.Instance);
            await tx.Commit();
        }
        Refresh(ref loadoutA);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        await Synchronizer.RescanFiles(GameInstallation);
        Refresh(ref loadoutA);
        
        LogDiskState(sb, "## 5 - Disabled ModB in Loadout(A) - Synced",
            """
            Disabled ModB in Loadout A and synced it. All the ModB files should have been removed from the disk state, except for the shared file.
            Files from ModA should still be present.
            """, [loadoutA]);
        
        await Verify(sb.ToString(), extension: "md");

    }
}
