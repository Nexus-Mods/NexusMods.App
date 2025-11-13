using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class ExternalChangesTests : ACyberpunkIsolatedGameTest<ExternalChangesTests>
{
    private readonly UniversalStubbedGameLocator<Cyberpunk2077Game> _locator;

    public ExternalChangesTests(ITestOutputHelper helper) : base(helper)
    {
        var locators = ServiceProvider.GetServices<IGameLocator>();
        _locator = locators.Should().ContainSingle().Which.Should().BeOfType<UniversalStubbedGameLocator<Cyberpunk2077Game>>().Which;
    }

    [Fact]
    public async Task CanDeployGameAfterUpdating()
    {
        var sb = new StringBuilder();
        await Synchronizer.RescanFiles(GameInstallation);
        var loadoutA = await CreateLoadout();

        _locator.LocatorIds.Should().ContainSingle().Which.Value.Should().Be("StubbedGameState.zip");
        loadoutA.GameVersion.Should().Be("1.0.Stubbed");
        
        LogDiskState(sb, "## 1 - Loadout Created (A) - Synced",
            """
            A new loadout has been created
            """, [loadoutA]);

        var gameFolder = loadoutA.InstallationInstance.Locations[LocationId.Game].Path;
        await ExtractV2ToGameFolder(gameFolder);

        _locator.LocatorIds.Should().ContainSingle().Which.Value.Should().Be("StubbedGameState_game_v2.zip");
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

        LoadoutItem.FindByLoadout(loadoutA.Db, loadoutA).OfTypeLoadoutItemWithTargetPath()
            .Select(i => (GamePath)i.TargetPath)
            .Should()
            .NotContain(changedFiles, "the files should not go into overrides or mods, but should update the game version");
        
        loadoutA.GameVersion.Should().Be("1.1.Stubbed");
        
        await Verify(sb.ToString(), extension: "md");
    }

    [Fact]
    public async Task ExistingFilesEndUpInOverrides()
    {
        // Get the game folder
        var gameFolder = GameInstallation.Locations[LocationId.Game].Path;
        
        await ExtractV2ToGameFolder(gameFolder);

        var extraFileName = gameFolder / "someFolder/SomeRandomFile.dds";
        extraFileName.Parent.CreateDirectory();
        await extraFileName.WriteAllTextAsync("Some content");

        
        // Create a new loadout
        var loadoutA = await CreateLoadout();
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        // Check the game version
        loadoutA.GameVersion.Should().Be("1.1.Stubbed");

        // Check that the extra file is in the overrides folder and there should only be one such file
        var extraFileGamePath = loadoutA.InstallationInstance.Locations.ToGamePath(extraFileName);
        var extraFileRecord =  LoadoutItem.FindByLoadout(loadoutA.Db, loadoutA).OfTypeLoadoutItemWithTargetPath().Single(f => f.TargetPath == extraFileGamePath);
        extraFileRecord.AsLoadoutItem().Parent.AsLoadoutItem().Name.Should().Be("Overrides", "the file should be in the overrides folder");
        
        // Delete the extra file
        extraFileName.Delete();
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        // Check that the extra file is no longer in the loadout. We can get an error here if reified deletes don't consider that the files they are
        // deleting may exist only in the overrides mod. In which case they should delete the entry instead.
        LoadoutItem.FindByLoadout(loadoutA.Db, loadoutA).OfTypeLoadoutItemWithTargetPath().Select(f => (GamePath)f.TargetPath)
            .Should()
            .NotContain(extraFileGamePath);
        
    }

    /// <summary>
    /// (issue-2908) Changes to external files should be reflected in the external files after synchronizing
    /// </summary>
    [Fact]
    public async Task ChangingExternalFileUpdatesExternalFiles()
    {
        await Synchronizer.RescanFiles(GameInstallation);
        var loadoutA = await CreateLoadout();
        var externalFile = loadoutA.InstallationInstance.Locations[LocationId.Game].Path / "config.json";
        var gameFile = new GamePath(LocationId.Game, "config.json");

        await externalFile.WriteAllTextAsync("version1");
        
        var loadout = await Synchronizer.Synchronize(loadoutA);
        
        var externalFileRecord =  LoadoutItem.FindByLoadout(loadoutA.Db, loadoutA).OfTypeLoadoutItemWithTargetPath().Single(f => f.TargetPath == gameFile);
        if (!externalFileRecord.TryGetAsLoadoutFile(out var loadoutFile))
            Assert.Fail("The file should be in the loadout");

        loadoutFile.Hash.Should().Be("version1".xxHash3AsUtf8());
        
        await externalFile.WriteAllTextAsync("version2");
        
        loadout = await Synchronizer.Synchronize(loadout);
        
        var refreshedRecord =  LoadoutItem.FindByLoadout(loadoutA.Db, loadoutA).OfTypeLoadoutItemWithTargetPath().Single(f => f.TargetPath == gameFile);
        refreshedRecord.Id.Should().Be(externalFileRecord.Id, "the file should be the same id");
        
        if (!refreshedRecord.TryGetAsLoadoutFile(out var refreshedFile))
            Assert.Fail("The file should be in the loadout");
        
        refreshedFile.Hash.Should().Be("version2".xxHash3AsUtf8());
        
    }

    private async Task ExtractV2ToGameFolder(AbsolutePath gameFolder)
    {
        _locator.LocatorIds = [LocatorId.From("StubbedGameState_game_v2.zip")];

        // Extract the game files into the folder so that we trigger a game version update
        using var zipFile = ZipFile.OpenRead((FileSystem.GetKnownPath(KnownPath.EntryDirectory) / "Resources/StubbedGameState_game_v2.zip").ToString());
        foreach (var file in zipFile.Entries)
        {
            if (!file.FullName.StartsWith("game") || file.Length == 0)
                continue;
            var path = RelativePath.FromUnsanitizedInput(string.Join("/", RelativePath.FromUnsanitizedInput(file.FullName).Parts.Skip(1).ToArray()));
            var gamePath = gameFolder / path;
            gamePath.Parent.CreateDirectory();
            await using var stream = gamePath.Create();
            await using var srcStream = file.Open();
            await srcStream.CopyToAsync(stream);
        }
    }
}
