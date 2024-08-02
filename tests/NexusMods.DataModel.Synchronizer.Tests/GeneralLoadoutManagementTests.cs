using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Games.TestFramework.FluentAssertionExtensions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class GeneralLoadoutManagementTests : AGameTest<Cyberpunk2077Game>
{
    public GeneralLoadoutManagementTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [Fact]
    public async Task CanCreateLoadouts()
    {
        var loadout = await CreateLoadout(false);
        loadout = await Synchronizer.Synchronize(loadout);
        
        var gamePath = new GamePath(LocationId.Game, "foo/bar.dds");
        var fullPath = GameInstallation.LocationsRegister.GetResolvedPath(gamePath);
        fullPath.Parent.CreateDirectory();
        await fullPath.WriteAllTextAsync("Hello World!");
        
        loadout = await Synchronizer.Synchronize(loadout);
        loadout.Items.Should().ContainItemTargetingPath(gamePath, "The file exists");
        
        fullPath.FileExists.Should().BeTrue("because the loadout was synchronized");

        await Synchronizer.ResetToOriginalGameState(loadout.InstallationInstance);
        fullPath.FileExists.Should().BeFalse("because the loadout was reset");

    }

}
