using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.StardewValley.Tests;

public class StardewValleySynchronizerTests(IServiceProvider serviceProvider) : AGameTest<StardewValley>(serviceProvider)
{
    [Fact]
    public async Task FilesInModFoldersAreMovedIntoMods()
    {
        var loadout = await CreateLoadoutOld();
        loadout = await SynchronizerOld.Synchronize(loadout);

        using var tx = Connection.BeginTransaction();

        var mod = new Mod.New(tx)
        {
            LoadoutId = loadout,
            Name = "Test Mod",
            Revision = 0,
            Enabled = true,
            Category = ModCategory.Mod,
            Status = ModStatus.Installed,
        };

        var manifestData = "{}";
        var manifestHash = manifestData.XxHash64AsUtf8();
        
        var manfiestFile = new StoredFile.New(tx, out var id)
        {
            File = new File.New(tx, id)
            {
                To = new GamePath(LocationId.Game, "Mods/test_mod_42/manifest.json".ToRelativePath()),
                ModId = mod,
                LoadoutId = loadout,
            },
            Hash = manifestHash,
            Size = Size.FromLong(manifestData.Length),
        };
        
        var result = await tx.Commit();

        var newModId = result.Remap(mod).Id;

        loadout = loadout.Rebase();
        loadout = await SynchronizerOld.Synchronize(loadout);
        
        var newFilePath = new GamePath(LocationId.Game, "Mods/test_mod_42/foo.dat".ToRelativePath());

        var absPath = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(newFilePath);

        absPath.Parent.CreateDirectory();
        await absPath.WriteAllTextAsync("Hello, World!");
        
        loadout = await SynchronizerOld.Synchronize(loadout);
        
        loadout.Files
            .TryGetFirst(f => f.To == newFilePath, out var found)
            .Should().BeTrue("The file was ingested from the game folder");

        found.Mod.Name.Should().Be("Test Mod", "The file was ingested into the parent mod folder");
        found.Mod.Id.Should().Be(newModId, "The file was ingested into the parent mod folder");
    }

    [Fact]
    public async Task ContentIsIgnoredWhenSettingIsSet()
    {
        // Get the settings
        var settings = ServiceProvider.GetRequiredService<ISettingsManager>().Get<StardewValleySettings>();
        settings.DoFullGameBackup = false;
        
        // Setup the paths we want to edit, one will be in the `Content` folder, thus not backed up
        var ignoredGamePath = new GamePath(LocationId.Game, "Content/foo.dat".ToRelativePath());
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
        var loadout = await CreateLoadoutOld();
        
        loadout.Files.Should().Contain(f => f.To == ignoredGamePath, "The file exists, but is ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeFalse("The file is ignored");
        
        loadout.Files.Should().Contain(f => f.To == notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored"); 
        
        // Now disable the ignore setting
        settings.DoFullGameBackup = true;

        var loadout2 = await CreateLoadoutOld();
        
        loadout2.Files.Should().Contain(f => f.To == ignoredGamePath, "The file exists, but is ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeTrue("The file is not ignored");
        loadout2.Files.Should().Contain(f => f.To == notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored");
    }

}
