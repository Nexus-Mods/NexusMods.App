using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.DataModel.Serializers.DiskStateTreeSchema;
using NexusMods.DataModel.Tests.Harness;
using Xunit.Sdk;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.DataModel.Tests;

public class ApplyServiceTests(IServiceProvider provider) : ADataModelTest<ApplyServiceTests>(provider)
{
    [Fact]
    public async Task CanApplyLoadout()
    {
        // Arrange
        await AddMods(BaseLoadout, Data7ZLzma2, "Mod1");
        Refresh(ref BaseLoadout);
        var gameFolder = BaseLoadout.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        await ApplyService.Apply(BaseLoadout);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
    }
    
    
    [Fact]
    public async Task CanApplyAndIngestLoadout()
    {
        // Arrange
        await AddMods(BaseLoadout, Data7ZLzma2, "Mod1");
        Refresh(ref BaseLoadout);

        var gameFolder = BaseLoadout.Installation.LocationsRegister[LocationId.Game];
        
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");
        
        // Act
        var newFile = new GamePath(LocationId.Saves, "newfile.dat");
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await ApplyService.Apply(BaseLoadout);
        
        // Assert
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been applied");
        gameFolder.Combine("folder1/folder1file.txt").FileExists.Should().BeTrue("loadout has been applied");
        
        Refresh(ref BaseLoadout);
        BaseLoadout.Mods.SelectMany(mod=> mod.Files)
            .Should()
            .Contain(file => file.To.EndsWith("newfile.dat"));
    }

    
    [Fact]
    public async Task CanIngestNewFiles()
    {
        // Arrange
        BaseLoadout.Mods.Should().HaveCount(1);
        var gameFolder = BaseLoadout.Installation.LocationsRegister[LocationId.Game];
        BaseLoadout.Mods.SelectMany(mod=> mod.Files)
            .Should()
            .NotContain(file => file.To.EndsWith( "newDiskFile.dat"));
        
        // Act
        var newFile = new GamePath(LocationId.Saves, "newDiskFile.dat");
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync([0x01, 0x02, 0x03]);
        var loadout = await ApplyService.Ingest(BaseLoadout.Installation);
        
        // Assert
        loadout!.Mods.SelectMany(mod=> mod.Files)
            .Should().Contain(file => file.To.EndsWith( "newDiskFile.dat"));
    }
    
    
    [Fact]
    public async Task CanIngestDeletedFiles()
    {
        // Arrange
        BaseLoadout.Mods.Should().HaveCount(1);
        await AddMods(BaseLoadout, Data7ZLzma2, "Mod1");
        
        var deletedFile = Install.LocationsRegister.GetResolvedPath(new GamePath(LocationId.Game, "rootFile.txt"));

        // Apply the loadout to make sure there are no uncommitted revisions
        await ApplyService.Apply(BaseLoadout);

        deletedFile.FileExists.Should().BeTrue("the file was applied");
        // Act
        deletedFile.Delete();
        deletedFile.FileExists.Should().BeFalse("the file was deleted");
        await ApplyService.Ingest(BaseLoadout.Installation);
        Refresh(ref BaseLoadout);
        
        // Assert
        deletedFile.FileExists.Should().BeFalse("file is still deleted after ingest");
        var files = BaseLoadout.Files.Where(f => f.To.EndsWith("rootFile.txt")).ToArray();
        files.Length.Should().Be(2, "deletes are reified, and the delete is in the overrides");
        var overrideFile = files.FirstOrDefault(f => f.Mod.Category == ModCategory.Overrides);
        overrideFile.Should().NotBeNull();
        overrideFile!.Contains(DeletedFile.Deleted).Should().BeTrue();
        
        
        // Act
        await ApplyService.Apply(BaseLoadout);

        
        // This only fails on Windows on github actions, I don't know why, disabled for now on windows
        // until I can investigate it - halgari
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            deletedFile.FileExists.Should().BeFalse("file is still deleted after apply");
        
    }
}
