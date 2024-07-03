using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.TestFramework;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.RedEngine.Tests;

public class Cyberpunk2077SynchronizerTests(IServiceProvider serviceProvider) : AGameTest<Cyberpunk2077>(serviceProvider)
{

    [Fact]
    public async Task ContentIsIgnoredWhenSettingIsSet()
    {
        // Get the settings
        var settings = ServiceProvider.GetRequiredService<ISettingsManager>().Get<Cyberpunk2077Settings>();
        settings.IgnoreContentFolder = true;
        
        // Setup the paths we want to edit, one will be in the `Content` folder, thus not backed up
        var ignoredGamePath = new GamePath(LocationId.Game, "archive/pc/content/foo.dat".ToRelativePath());
        var notIgnoredGamePath = new GamePath(LocationId.Game, "foo.dat".ToRelativePath());
        
        var ignoredPath = GameInstallation.LocationsRegister.GetResolvedPath(ignoredGamePath);
        ignoredPath.Parent.CreateDirectory();
        var notIgnoredPath = GameInstallation.LocationsRegister.GetResolvedPath(notIgnoredGamePath);
        
        // Write the files
        await ignoredPath.WriteAllTextAsync("Ignore me");
        var ignoredHash = await ignoredPath.XxHash64Async();
        await notIgnoredPath.WriteAllTextAsync("Don't you dare ignore me!");
        var notIgnoredHash = await notIgnoredPath.XxHash64Async();
        
        // Create the loadout
        var loadout = await CreateLoadout();
        
        loadout.Files.Should().Contain(f => f.To == ignoredGamePath, "The file exists, but is ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeFalse("The file is ignored");
        
        loadout.Files.Should().Contain(f => f.To == notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored"); 
        
        // Now disable the ignore setting
        settings.IgnoreContentFolder = false;

        var loadout2 = await CreateLoadout();
        
        loadout2.Files.Should().Contain(f => f.To == ignoredGamePath, "The file exists, but is ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeTrue("The file is not ignored");
        loadout2.Files.Should().Contain(f => f.To == notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored");
    }

}
