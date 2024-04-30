using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.DataModel.Tests.Harness;
using Xunit.Sdk;

namespace NexusMods.DataModel.Tests;

public class ApplyServiceTests(IServiceProvider provider) : ADataModelTest<ApplyServiceTests>(provider)
{
    [Fact]
    public async Task CanApplyLoadout()
    {
        // Arrange
        await AddMods(BaseLoadout, Data7ZLzma2, "Mod1");
        throw new NotImplementedException();
        /*
        var loadout = BaseLoadout.Value;
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        var result = await ApplyService.Apply(loadout.LoadoutId);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
        */
        
    }
    
    /*
    [Fact]
    public async Task CanApplyAndIngestLoadout()
    {
        // Arrange
        await AddMods(BaseList, Data7ZLzma2, "Mod1");
        var loadout = BaseList.Value;
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        var newFile = new GamePath(LocationId.Saves, "newfile.dat");
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        var result = await ApplyService.Apply(loadout.LoadoutId);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
        
        var loadoutResult = LoadoutRegistry.Get(result.LoadoutId);
        loadoutResult.Should().NotBeNull();
        loadoutResult!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().Contain(file => file.To.EndsWith( "newFile.dat"));
    }

    [Fact]
    public async Task CanIngestNewFiles()
    {
        // Arrange
        BaseList.Value.Mods.Should().HaveCount(1);
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        BaseList.Value.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().NotContain(file => file.To.EndsWith( "newDiskFile.dat"));
        
        // Act
        var newFile = new GamePath(LocationId.Saves, "newDiskFile.dat");
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        var result = await ApplyService.Ingest(BaseList.Value.Installation);
        
        // Assert
        var loadout = LoadoutRegistry.Get(result.LoadoutId);
        loadout.Should().NotBeNull();
        loadout!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().Contain(file => file.To.EndsWith( "newDiskFile.dat"));
    }
    
    [Fact]
    public async Task CanIngestDeletedFiles()
    {
        // Arrange
        BaseList.Value.Mods.Should().HaveCount(1);
        await AddMods(BaseList, Data7ZLzma2, "Mod1");
        var deletedFile = new GamePath(LocationId.Game, "rootFile.txt");
        // Apply the loadout to make sure there are no uncommitted revisions
        await ApplyService.Apply(BaseList.Value.LoadoutId);

        // Act
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        var result = await ApplyService.Ingest(BaseList.Value.Installation);
        
        // Assert
        var loadout = LoadoutRegistry.Get(result.LoadoutId);
        loadout.Should().NotBeNull();
        loadout!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().NotContain(file => file.To.EndsWith( "rootFile.txt"));
    }
    */
}
