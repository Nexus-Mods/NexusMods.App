using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.DataModel.Tests.Harness;

namespace NexusMods.DataModel.Tests;

public class ApplyServiceTests : ADataModelTest<ApplyServiceTests>
{
    private readonly IApplyService _applyService;
    private readonly ILoadoutRegistry _loadoutRegistry;
    
    public ApplyServiceTests(IServiceProvider provider) : base(provider)
    {
        _applyService = provider.GetRequiredService<IApplyService>();
        _loadoutRegistry = provider.GetRequiredService<ILoadoutRegistry>();
    }
    
    [Fact]
    public async Task CanApplyLoadout()
    {
        // Arrange
        await AddMods(BaseList, Data7ZLzma2, "Mod1");
        var loadout = BaseList.Value;
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        var result = await _applyService.Apply(loadout.LoadoutId);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
    }
    
    [Fact]
    public async Task CanApplyAndIngestLoadout()
    {
        // Arrange
        await AddMods(BaseList, Data7ZLzma2, "Mod1");
        var loadout = BaseList.Value;
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        FileSystem.CreateFile(gameFolder.Combine("newFile.txt"));
        var result = await _applyService.Apply(loadout.LoadoutId);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
        
        var loadoutResult = _loadoutRegistry.Get(result.LoadoutId);
        loadoutResult.Should().NotBeNull();
        loadoutResult!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().Contain(file => file.To.EndsWith( "newFile.txt"));
    }

    [Fact]
    public async Task CanIngestNewFiles()
    {
        // Arrange
        BaseList.Value.Mods.Should().HaveCount(1);
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        BaseList.Value.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().NotContain(file => file.To.EndsWith( "newDiskFile.txt"));
        
        // Act
        FileSystem.CreateFile(gameFolder.Combine("newDiskFile.txt"));
        var result = await _applyService.Ingest(BaseList.Value.Installation);
        
        // Assert
        var loadout = _loadoutRegistry.Get(result.LoadoutId);
        loadout.Should().NotBeNull();
        loadout!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().Contain(file => file.To.EndsWith( "newDiskFile.txt"));
    }
    
    [Fact]
    public async Task CanIngestDeletedFiles()
    {
        // Arrange
        BaseList.Value.Mods.Should().HaveCount(1);
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];
        gameFolder.Combine("config.ini").FileExists.Should().BeTrue("base game file was not deleted yet");

        // Act
        FileSystem.DeleteFile(gameFolder.Combine("config.ini"));
        gameFolder.Combine("config.ini").FileExists.Should().BeFalse("File was deleted");
        var result = await _applyService.Ingest(BaseList.Value.Installation);
        
        // Assert
        var loadout = _loadoutRegistry.Get(result.LoadoutId);
        loadout.Should().NotBeNull();
        loadout!.Mods.Values.SelectMany(mod=> mod.Files.Values).OfType<IToFile>()
            .Should().NotContain(file => file.To.EndsWith( "config.ini"));
    }
}
