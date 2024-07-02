using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
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
        var loadout = await CreateLoadout();
        loadout = await Synchronizer.Synchronize(loadout);

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
        
        var manfiestFile = new StoredFile.New(tx)
        {
            File = new File.New(tx)
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
        loadout = await Synchronizer.Synchronize(loadout);
        
        var newFilePath = new GamePath(LocationId.Game, "Mods/test_mod_42/foo.dat".ToRelativePath());

        var absPath = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(newFilePath);

        absPath.Parent.CreateDirectory();
        await absPath.WriteAllTextAsync("Hello, World!");
        
        loadout = await Synchronizer.Synchronize(loadout);
        
        loadout.Files
            .TryGetFirst(f => f.To == newFilePath, out var found)
            .Should().BeTrue("The file was ingested from the game folder");

        found.Mod.Name.Should().Be("Test Mod", "The file was ingested into the parent mod folder");
        found.Mod.Id.Should().Be(newModId, "The file was ingested into the parent mod folder");
    }

}
