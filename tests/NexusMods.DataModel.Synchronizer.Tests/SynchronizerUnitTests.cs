using FluentAssertions;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

/// <summary>
/// Tests for specific issues or regressions in the Synchronizer.
/// </summary>
public class SynchronizerUnitTests(ITestOutputHelper testOutputHelper) : ACyberpunkIsolatedGameTest<SynchronizerUnitTests>(testOutputHelper)
{
    [Fact]
    [GithubIssue(1925)]
    public async Task EmptyChildFoldersDontDeleteNonEmptyParents()
    {
        var loadout = await CreateLoadout();
        
        var parentFile = new GamePath(LocationId.Game, "a/parent.txt");
        var grandChildFile = new GamePath(LocationId.Game, "a/b/c/grandchild.txt");
        
        var parentFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(parentFile);
        var grandChildFileFullPath = GameInstallation.LocationsRegister.GetResolvedPath(grandChildFile);
        
        parentFileFullPath.Parent.CreateDirectory();
        await parentFileFullPath.WriteAllTextAsync("Parent File");
        
        grandChildFileFullPath.Parent.CreateDirectory();
        await grandChildFileFullPath.WriteAllTextAsync("Grand Child File");
        
        loadout = await Synchronizer.Synchronize(loadout);
        
        loadout.Items.Should().ContainSingle(f => f.Name == "parent.txt");
        loadout.Items.Should().ContainSingle(f => f.Name == "grandchild.txt");
        


        using (var tx = Connection.BeginTransaction())
        {
            var toDelete = loadout.Items.First(f => f.Name == "grandchild.txt").Id;
            tx.Delete(toDelete, false);
            await tx.Commit();
        }

        loadout = loadout.Rebase();
        loadout = await Synchronizer.Synchronize(loadout);
        
        grandChildFileFullPath.FileExists.Should().BeFalse();
        parentFileFullPath.FileExists.Should().BeTrue();
    }

    [Fact]
    public async Task LoadoutsContainLocatorMetadata()
    {
        var loadout = await CreateLoadout();
        loadout.LocatorIds.Should().BeEquivalentTo(["StubbedGameState.zip"]);
    }
}
