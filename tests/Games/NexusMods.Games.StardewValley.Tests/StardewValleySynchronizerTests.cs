using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Games.TestFramework.FluentAssertionExtensions;

namespace NexusMods.Games.StardewValley.Tests;

public class StardewValleySynchronizerTests(IServiceProvider serviceProvider) : AGameTest<StardewValley>(serviceProvider)
{
    [Fact]
    public void ContentIsIgnoredWhenSettingIsSet()
    {
        // Get the settings
        var settings = ServiceProvider.GetRequiredService<ISettingsManager>().Get<StardewValleySettings>();
        settings.DoFullGameBackup = false;
        
        // Setup the paths we want to edit, one will be in the `Content` folder, thus not backed up
        var ignoredGamePath = new GamePath(LocationId.Game, "Content/foo.dat");
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
