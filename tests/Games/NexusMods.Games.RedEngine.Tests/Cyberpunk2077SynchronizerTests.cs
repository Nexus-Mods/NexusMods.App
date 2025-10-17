using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Sdk.Settings;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.RedEngine.Tests;

public class Cyberpunk2077SynchronizerTests(IServiceProvider serviceProvider) : AGameTest<Cyberpunk2077.Cyberpunk2077Game>(serviceProvider)
{

    [Fact]
    public async Task ContentIsIgnoredWhenSettingIsSet()
    {
        // Get the settings
        var settings = ServiceProvider.GetRequiredService<ISettingsManager>().Get<Cyberpunk2077Settings>();
        settings.DoFullGameBackup = false;
        
        // Setup the paths we want to edit, one will be in the `Content` folder, thus not backed up
        var ignoredGamePath = new GamePath(LocationId.Game, "archive/pc/content/foo.dat");
        var notIgnoredGamePath = new GamePath(LocationId.Game, "foo.dat");
        
        // Check if the paths are ignored
        Synchronizer.IsIgnoredBackupPath(ignoredGamePath).Should().BeTrue("The setting is now disabled");
        Synchronizer.IsIgnoredBackupPath(notIgnoredGamePath).Should().BeFalse("The setting is now disabled");
        
        // Now disable the ignore setting
        settings.DoFullGameBackup = true;
        Synchronizer.IsIgnoredBackupPath(ignoredGamePath).Should().BeFalse("The setting is now disabled");
        Synchronizer.IsIgnoredBackupPath(notIgnoredGamePath).Should().BeFalse("The setting is now disabled");
    }
}
