using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralLoadoutManagementTests : AGameTest<Cyberpunk2077Game>
{
    private ILogger<GeneralLoadoutManagementTests> _logger;
    public GeneralLoadoutManagementTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GeneralLoadoutManagementTests>>();
    }

    [Fact]
    public async Task SynchronizerIntegrationTests()
    {
        var sb = new StringBuilder();
        
        var originalFileGamePath = new GamePath(LocationId.Game, "bin/originalGameFile.txt");
        var originalFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(originalFileGamePath);
        originalFileFullPath.Parent.CreateDirectory();
        await originalFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        
        LogState(sb, "## 1 - Initial State",
            """
            The initial state of the game folder should contain the game files as they were created by the game store. No loadout has been created yet.
            """);
        var loadoutA = await CreateLoadout(false);

        LogState(sb, "## 2 - Loadout Created (A) - Synced",
            """
            A new loadout has been created and has been synchronized, so the 'Last Synced State' should be set to match the new loadout.
            """, [loadoutA]);

        var newFileInGameFolderA = new GamePath(LocationId.Game, "bin/newFileInGameFolderA.txt");
        var newFileFullPathA = GameInstallation.LocationsRegister.GetResolvedPath(newFileInGameFolderA);
        newFileFullPathA.Parent.CreateDirectory();
        await newFileFullPathA.WriteAllTextAsync("New File for this loadout");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        LogState(sb, "## 4 - New File Added to Game Folder",
            """
            New files have been added to the game folder by the user or the game, but the loadout hasn't been synchronized yet.
            """, [loadoutA]);

        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        LogState(sb, "## 5 - Loadout Synced with New File",
            """
            After the loadout has been synchronized, the new file should be added to the loadout.
            """, [loadoutA]);
        
        
        await Synchronizer.DeactivateCurrentLoadout(GameInstallation);
        LogState(sb, "## 6 - Loadout Deactivated", 
            """
            At this point the loadout is deactivated, and all the files in the current state should match the initial state.
            """, [loadoutA]);
        
        
        var loadoutB = await CreateLoadout(false);

        LogState(sb, "## 7 - New Loadout (B) Created - No Sync",
            """
            A new loadout is created, but it has not been synchronized yet. So again the 'Last Synced State' is not set.
            """, [loadoutA, loadoutB]
        );
        
        loadoutB = await Synchronizer.Synchronize(loadoutB);
        
        LogState(sb, "## 8 - New Loadout (B) Synced",
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
        
        LogState(sb, "## 9 - New File Added to Game Folder (B)",
            """
            A new file has been added to the game folder and B loadout has been synchronized. The new file should be added to the B loadout.
            """, [loadoutA, loadoutB]
        );
        

        await Synchronizer.DeactivateCurrentLoadout(GameInstallation);
        await Synchronizer.ActivateLoadout(loadoutA);
        
        LogState(sb, "## 10 - Switch back to Loadout A",
            """
            Now we switch back to the A loadout, and the new file should be removed from the game folder.
            """, [loadoutA, loadoutB]
        );
        
        var loadoutC = await Synchronizer.CopyLoadout(loadoutA);
        
        LogState(sb, "## 11 - Loadout A Copied to Loadout C",
            """
            Loadout A has been copied to Loadout C, and the contents should match.
            """, [loadoutA, loadoutB, loadoutC]
        );
        
        await Synchronizer.UnManage(GameInstallation);
        
        LogState(sb, "## 12 - Game Unmanaged",
            """
            The loadouts have been deleted and the game folder should be back to its initial state.
            """,
        [loadoutA.Rebase(), loadoutB.Rebase()]);
        
        await Verify(sb.ToString(), extension: "md");
    }

    /// <summary>
    /// Uses Verify to validate all the states of the game, previously applied states, etc.
    /// </summary>
    private void LogState(StringBuilder sb, string sectionName, string comments = "", Loadout.ReadOnly[]? loadouts = null)
    {
        _logger.LogInformation("Logging State {SectionName}", sectionName);
        
        var metadata = GameInstallation.GetMetadata(Connection);
        sb.AppendLine($"{sectionName}:");
        if (!string.IsNullOrEmpty(comments))
            sb.AppendLine(comments);
        
        Section("### Initial State", metadata.InitialDiskStateTransaction);
        if (metadata.Contains(GameInstallMetadata.LastSyncedLoadoutTransaction)) 
            Section("### Last Synced State", metadata.LastSyncedLoadoutTransaction);
        Section("### Current State", metadata.LastScannedDiskStateTransaction);
        foreach (var loadout in loadouts ?? [])
        {
            if (!loadout.Items.Any())
                continue;

            var files = loadout.Items.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile().ToArray();
            
            sb.AppendLine($"### Loadout {loadout.ShortName} - ({files.Length})");
            sb.AppendLine("| Path | Hash | Size | TxId |");
            sb.AppendLine("| --- | --- | --- | --- |");
            foreach (var entry in files) 
                sb.AppendLine($"| {entry.AsLoadoutItemWithTargetPath().TargetPath} | {entry.Hash} | {entry.Size} | {entry.MaxBy(x => x.T)?.T.ToString()} |");
        }
        sb.AppendLine("\n\n");
        
        void Section(string sectionName, Transaction.ReadOnly asOf)
        {
            var entries = metadata.DiskStateAsOf(asOf);
            sb.AppendLine($"{sectionName} - ({entries.Count}) - {TxId.From(asOf.Id.Value)}");
            sb.AppendLine("| Path | Hash | Size | TxId |");
            sb.AppendLine("| --- | --- | --- | --- |");
            foreach (var entry in entries) 
                sb.AppendLine($"| {entry.Path} | {entry.Hash} | {entry.Size} | {entry.MaxBy(x => x.T)?.T.ToString()} |");
        }
        
    }

}
