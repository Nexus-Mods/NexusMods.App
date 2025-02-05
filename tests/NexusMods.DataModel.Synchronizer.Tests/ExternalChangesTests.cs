using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class ExternalChangesTests (ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{
    [Fact]
    public async Task CanDeployGameAfterUpdating()
    {
        var sb = new StringBuilder();
        await Synchronizer.RescanFiles(GameInstallation);
        var loadoutA = await CreateLoadout();
        loadoutA.GameVersion.Should().Be("1.0.Stubbed");
        
        LogDiskState(sb, "## 1 - Loadout Created (A) - Synced",
            """
            A new loadout has been created
            """, [loadoutA]);

        var gameFolder = loadoutA.InstallationInstance.LocationsRegister[LocationId.Game];
        await FileExtractor.ExtractAllAsync(FileSystem.GetKnownPath(KnownPath.EntryDirectory) / "Resources/StubbedGameState_game_v2.zip", gameFolder);
        
        loadoutA = await Synchronizer.Synchronize(loadoutA);

        LogDiskState(sb, "## 2 - Updated Game Files",
            """
            The game files have been updated to a new version of the game, they should not make it into the loadout.
            """, [loadoutA]);

        var changedFiles = new[]
        {
            new GamePath(LocationId.Game, "game/Data/image.dds"),
            new GamePath(LocationId.Game, "game/Data/image2.dds"),
        };
        
        loadoutA.Items.OfTypeLoadoutItemWithTargetPath().Select(i => (GamePath)i.TargetPath)
            .Should()
            .NotContain(changedFiles, "the files should not go into overrides or mods, but should update the game version");
        
        loadoutA.GameVersion.Should().Be("1.1.Stubbed");
        
        await Verify(sb.ToString(), extension: "md");
    }
    
}
