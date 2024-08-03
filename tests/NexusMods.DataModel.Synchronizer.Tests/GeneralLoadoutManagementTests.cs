using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Games.TestFramework.FluentAssertionExtensions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralLoadoutManagementTests : AGameTest<Cyberpunk2077Game>
{
    public GeneralLoadoutManagementTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [Fact]
    public async Task CanResetToGameOriginalState()
    {
        var sb = new StringBuilder();
        
        var originalFileGamePath = new GamePath(LocationId.Game, "bin/originalGameFile.txt");
        var originalFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(originalFileGamePath);
        originalFileFullPath.Parent.CreateDirectory();
        await originalFileFullPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        
        LogState(sb, "## 1 - Initial State");
        var loadout = await CreateLoadout(false);
        
        LogState(sb, "## 2 - Loadout Created - No Sync");
        loadout = await Synchronizer.Synchronize(loadout);

        await Synchronizer.Synchronize(loadout);
        LogState(sb, "## 3 - Loadout Created - Synced");
        
        var newFileInGameFolder = new GamePath(LocationId.Game, "bin/newFileInGameFolder.txt");
        var newFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(newFileInGameFolder);
        newFileFullPath.Parent.CreateDirectory();
        await newFileFullPath.WriteAllTextAsync("New File for this loadout");
        
        await Synchronizer.RescanGameFiles(GameInstallation);
        LogState(sb, "## 4 - New File Added to Game Folder");

        loadout = await Synchronizer.Synchronize(loadout);
        
        LogState(sb, "## 5 - Loadout Synced with New File");
        
        
        await Synchronizer.DeactivateCurrentLoadout(GameInstallation);
        LogState(sb, "## 6 - Loadout Deactivated");
        
        /*

        await VerifyAllStates().UseParameters("Initial State");
        */

        /*


        loadout = await Synchronizer.Synchronize(loadout);
        loadout.Items.Should().ContainItemTargetingPath(gamePath, "The file exists");

        fullPath.FileExists.Should().BeTrue("because the loadout was synchronized");
        (await fullPath.ReadAllTextAsync()).Should().Be("Hello World!", "because the file was written");

        await Synchronizer.ResetToOriginalGameState(loadout.InstallationInstance);
        fullPath.FileExists.Should().BeFalse("because the loadout was reset");

        await Synchronizer.Synchronize(loadout);
        fullPath.FileExists.Should().BeTrue("because the loadout was synchronized");
        (await fullPath.ReadAllTextAsync()).Should().Be("Hello World!", "because the file was written");

        await VerifyAllStates();
        */

        await Verify(sb.ToString(), extension: "md");
    }

    /// <summary>
    /// Uses Verify to validate all the states of the game, previously applied states, etc.
    /// </summary>
    private void LogState(StringBuilder sb, string sectionName, string comments = "")
    {
        var metadata = GameInstallation.GetMetadata(Connection);
        sb.AppendLine($"{sectionName}:");
        if (!string.IsNullOrEmpty(comments))
            sb.AppendLine(comments);
        
        Section("### Initial State", metadata.DiskStateAsOf(metadata.InitialStateTransaction));
        if (metadata.Contains(GameMetadata.LastAppliedLoadoutTransaction)) 
            Section("### Last Applied State", metadata.DiskStateAsOf(metadata.LastAppliedLoadoutTransaction));
        Section("### Current State", metadata.DiskStateAsOf(metadata.LastScannedTransaction));
        sb.AppendLine("\n\n");
        
        
        void Section(string sectionName, Entities<DiskStateEntry.ReadOnly> entries)
        {
            sb.AppendLine($"{sectionName} - ({entries.Count})");
            sb.AppendLine("| Path | Hash | Size |");
            sb.AppendLine("| --- | --- | --- |");
            foreach (var entry in entries) 
                sb.AppendLine($"| {entry.Path} | {entry.Hash} | {entry.Size} |");
        }
        
    }

}
