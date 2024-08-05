using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Games.TestFramework.FluentAssertionExtensions;

namespace NexusMods.Games.StardewValley.Tests;

public class StardewValleySynchronizerTests(IServiceProvider serviceProvider) : AGameTest<StardewValley>(serviceProvider)
{
    [Fact]
    public async Task FilesInModFoldersAreMovedIntoMods()
    {
        var loadout = await CreateLoadout();
        loadout = await Synchronizer.Synchronize(loadout);

        using var tx = Connection.BeginTransaction();
        
        var manifestData = "{}";
        var manifestHash = manifestData.XxHash64AsUtf8();
        
        var smapiMod = new SMAPIModLoadoutItem.New(tx, out var modId)
        {
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, modId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, modId)
                {
                    LoadoutId = loadout,
                    Name = "Test Mod",
                }
            },
            ManifestId = new SMAPIManifestLoadoutFile.New(tx, out var fileId)
            {
                IsManifestFile = true,
                LoadoutFile = new LoadoutFile.New(tx, fileId)
                {
                    Hash = manifestHash,
                    Size = Size.FromLong(manifestData.Length),
                    LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, fileId)
                    {
                        TargetPath = new GamePath(LocationId.Game, "Mods/test_mod_42/manifest.json".ToRelativePath()),
                        LoadoutItem = new LoadoutItem.New(tx, fileId)
                        {
                            LoadoutId = loadout,
                            ParentId = modId,
                            Name = "Test Mod - manifest.json",
                        },
                    },
                },
            },
        };
        
        var result = await tx.Commit();

        var newModId = result.Remap(smapiMod).Id;

        loadout = loadout.Rebase();
        loadout = await Synchronizer.Synchronize(loadout);
        
        var newFilePath = new GamePath(LocationId.Game, "Mods/test_mod_42/foo.dat".ToRelativePath());

        var absPath = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(newFilePath);

        absPath.Parent.CreateDirectory();
        await absPath.WriteAllTextAsync("Hello, World!");
        
        loadout = await Synchronizer.Synchronize(loadout);
        
        loadout.Items.Should().ContainItemTargetingPath(newFilePath, "The file was moved into the mod folder");
        var foundMod = loadout.Items
            .OfTypeLoadoutItemWithTargetPath().Where(f => f.TargetPath == newFilePath)
            .Select(f => f.AsLoadoutItem().Parent)
            .First();

        foundMod.AsLoadoutItem().Name.Should().Be("Test Mod", "The file was ingested into the parent mod folder");
        foundMod.Id.Should().Be(newModId, "The file was ingested into the parent mod folder");
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
        await ignoredPath.WriteAllTextAsync("Ignore me" + Guid.NewGuid());
        var ignoredHash = await ignoredPath.XxHash64Async();
        await notIgnoredPath.WriteAllTextAsync("Don't you dare ignore me!" + Guid.NewGuid());
        var notIgnoredHash = await notIgnoredPath.XxHash64Async();
        
        // Create the loadout
        var loadout = await CreateLoadout();
        
        loadout.Items.Should().ContainItemTargetingPath(ignoredGamePath, "The file exists, but is ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeFalse("The file is ignored");
        
        loadout.Items.Should().ContainItemTargetingPath(notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored"); 
        
        // Now disable the ignore setting
        settings.DoFullGameBackup = true;

        var loadout2 = await CreateLoadout();
        
        loadout2.Items.Should().ContainItemTargetingPath(ignoredGamePath, "The file is not ignored");
        (await FileStore.HaveFile(ignoredHash)).Should().BeTrue("The file is not ignored");
        loadout2.Items.Should().ContainItemTargetingPath(notIgnoredGamePath, "The file was not ignored");
        (await FileStore.HaveFile(notIgnoredHash)).Should().BeTrue("The file was not ignored");
    }

}
