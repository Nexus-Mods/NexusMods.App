using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Tests;

public class ALoadoutSynchronizerTests : ADataModelTest<ALoadoutSynchronizerTests>
{
    private readonly Dictionary<ModId, string> _modNames = new();
    private readonly Dictionary<string, ModId> _modIdForName = new();
    private readonly List<ModId> _modIds = new();
    private const int ModCount = 10;

    private static GamePath _texturePath = new(LocationId.Game, "textures/a.dds");
    private static GamePath _meshPath = new(LocationId.Game, "meshes/b.nif");
    private static GamePath _prefsPath = new(LocationId.Preferences, "preferences/settings.ini");
    private static GamePath _savePath = new(LocationId.Saves, "saves/save1.dat");
    private static GamePath _configPath = new(LocationId.Game, "config.ini");
    private static GamePath _imagePath = new(LocationId.Game, "Data/image.dds");

    private static GamePath[] _allPaths = {_texturePath , _meshPath, _prefsPath, _savePath};

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    public ALoadoutSynchronizerTests(IServiceProvider provider) : base(provider) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        {
            // Disable all mods
            using var tx = Connection.BeginTransaction();
            foreach (var mod in BaseLoadout.Mods)
                tx.Add(mod.Id, Mod.Enabled, false);
            await tx.Commit();
            Refresh(ref BaseLoadout);
        }


        for (var i = 0; i < ModCount; i++)
        {
            var modName = $"Mod {i}";
            var modId = await AddMod(modName,
                // Each mod overrides the same files for these three files
                (_texturePath.Path, $"mod{i}-texture"),
                (_meshPath.Path, $"mod{i}-mesh"),
                (_prefsPath.Path, $"mod{i}-prefs"),
                (_savePath.Path, $"mod{i}-save"),
                // When flattened, this will show as one file per mod
                ($"perMod/{i}.dat", $"mod{i}-perMod"),
                ("bin/script.sh", $"script{i}.sh"),
                ("bin/binary", $"binary{i}"));

            _modIdForName[modName] = modId;
            _modNames[modId] = modName;
            _modIds.Add(modId);
        }


        {
            using var tx = Connection.BeginTransaction();
            for (var i = 0; i < ModCount - 1; i++)
            {
                tx.Add(_modIds[i].Value, Mod.SortAfter, _modIds[i + 1].Value);
            }
            await tx.Commit();
        }

        Refresh(ref BaseLoadout);
    }
    
    [Fact]
    public async Task ApplyingTwiceDoesNothing()
    {
        // This test is mostly a vestige of the apply/ingest system, but it's still a good sanity check to make 
        // sure that the system is idempotent.
        
        await Synchronizer.Synchronize(BaseLoadout);
        await Synchronizer.Synchronize(BaseLoadout);

        // This should not throw as the disk state should be in sync

        using var tx = Connection.BeginTransaction();
        tx.Add(BaseLoadout.Id, Loadout.Name, "Changed Name");
        await tx.Commit();
        Refresh(ref BaseLoadout);
        
        await Synchronizer.Synchronize(BaseLoadout);
    }

    
    [Fact]
    public async Task ApplyingDeletesCleansUpEmptyDirectories()
    {
        await Synchronizer.Synchronize(BaseLoadout);

        var file1 = new GamePath(LocationId.Game, "deleteMeMod/deleteMeDir1/deleteMeFile.txt");
        var path1 = Install.LocationsRegister.GetResolvedPath(file1);
        var file2 = new GamePath(LocationId.Game, "deleteMeMod/deleteMeDir2/deleteMeFile.txt");
        var path2 = Install.LocationsRegister.GetResolvedPath(file2);

        // Add mod that will be deleted
        var toDelete = await AddMod("DeleteMe",
            (_texturePath.Path, "texture.dds"),
            (file1.Path, "deleteMeContents"),
            (file2.Path, "deleteMeContents"));

        Refresh(ref BaseLoadout);
        await Synchronizer.Synchronize(BaseLoadout);

        path1.FileExists.Should().BeTrue("the file should exist");

        // Delete the mod
        {
            using var tx = Connection.BeginTransaction();
            tx.Add(toDelete.Value, Mod.Enabled, false);
            await tx.Commit();
        }

        Refresh(ref BaseLoadout);
        await Synchronizer.Synchronize(BaseLoadout);

        path1.FileExists.Should().BeFalse("the file should not exist");
        path1.Parent.DirectoryExists().Should().BeFalse("the directory should not exist");
        path1.Parent.Parent.DirectoryExists().Should().BeFalse("the directory should not exist");

        path2.FileExists.Should().BeFalse("the file should not exist");
        path2.Parent.DirectoryExists().Should().BeFalse("the directory should not exist");

        var textureAbsPath = Install.LocationsRegister.GetResolvedPath(_texturePath.Parent);
        textureAbsPath.DirectoryExists().Should().BeTrue("the texture folder should still exist");
    }
    
    [Fact]
    public async Task CanFlattenLoadout()
    {
        var flattened = await Synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var rows = flattened.GetAllDescendentFiles()
            .Select(f => new
                {
                    Path = f.GamePath().ToString(),
                    Mod = f.Item.Value.Mod.Name.ToString(),
                }
            ).OrderBy(f => f.Path);
        
        await Verify(rows);
    }

    
    [Fact]
    public async Task CanCreateFileTree()
    {
        var flattened = await Synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var fileTree = await Synchronizer.FlattenedLoadoutToFileTree(flattened, BaseLoadout);

        var rows = fileTree.GetAllDescendentFiles()
            .Select(f => new
                {
                    Path = f.GamePath().ToString(),
                    Mod = f.Item.Value.Mod.Name.ToString(),
                }
            ).OrderBy(f => f.Path);
        
        await Verify(rows);
    }
    
    [Fact]
    public async Task CanSynchronizeLoadout()
    {
        await Synchronizer.Synchronize(BaseLoadout);
        
        var diskState = DiskStateRegistry.GetState(BaseLoadout.InstallationInstance)!;
        
        diskState.GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .BeEquivalentTo(new[]
                {
                    "{Game}/meshes/b.nif",
                    "{Game}/perMod/0.dat",
                    "{Game}/perMod/1.dat",
                    "{Game}/perMod/2.dat",
                    "{Game}/perMod/3.dat",
                    "{Game}/perMod/4.dat",
                    "{Game}/perMod/5.dat",
                    "{Game}/perMod/6.dat",
                    "{Game}/perMod/7.dat",
                    "{Game}/perMod/8.dat",
                    "{Game}/perMod/9.dat",
                    "{Game}/textures/a.dds",
                    "{Game}/bin/script.sh",
                    "{Game}/bin/binary",
                    "{Preferences}/preferences/settings.ini",
                    "{Saves}/saves/save1.dat"
                },
                "files have all been written to disk");

        foreach (var file in diskState.GetAllDescendentFiles())
        {
            var path = Install.LocationsRegister.GetResolvedPath(file.GamePath());
            path.FileExists.Should().BeTrue("the file should exist on disk");
            path.FileInfo.Size.Should().Be(file.Item.Value.Size, "the file size should match");
            (await path.XxHash64Async()).Should().Be(file.Item.Value.Hash, "the file hash should match");
        }

        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        var scriptPath = Install.LocationsRegister.GetResolvedPath(new GamePath(LocationId.Game, "bin/script.sh"));
        var binaryPath = Install.LocationsRegister.GetResolvedPath(new GamePath(LocationId.Game, "bin/binary"));
        var executeFlags = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        scriptPath.GetUnixFileMode().Should().HaveFlag(executeFlags);
        binaryPath.GetUnixFileMode().Should().HaveFlag(executeFlags);
        
    }

    
    [Fact]
    public async Task CanLoadoutToDiskDiff()
    {
        var prevDiskState = DiskStateRegistry.GetState(BaseLoadout.InstallationInstance)!;

        await AddMod("ReplacingConfigMod",
            // This replaces the config file without changing the contents
            (_configPath.Path, "config.ini"),
            // This replaces the image file with a new one
            (_imagePath.Path, "modifiedImage.dds")
            );

        Refresh(ref BaseLoadout);
        var diffTree = await Synchronizer.LoadoutToDiskDiff(BaseLoadout, prevDiskState );
        var res = diffTree.GetAllDescendentFiles()
            .Select(node => VerifiableFile.From(node.Item.Value))
            .OrderByDescending(mod => mod.GamePath)
            .ToArray();

        await Verify(res);
    }

    
    [Fact]
    public async Task CanIngestDiskState()
    {
        // Apply the old state
        await Synchronizer.Synchronize(BaseLoadout);

        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");
        var deletedFile = new GamePath(LocationId.Game, "perMod/9.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync([0x01, 0x02, 0x03]);
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync([0x04, 0x05, 0x06]);
        
        await Synchronizer.Synchronize(BaseLoadout);
        
        var diskState = DiskStateRegistry.GetState(Install)!;

        diskState
            .GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .BeEquivalentTo(new[]
                {
                    // modifiedFile: b.nif is modified, so it should be included
                    "{Game}/meshes/b.nif",
                    "{Game}/perMod/0.dat",
                    "{Game}/perMod/1.dat",
                    "{Game}/perMod/2.dat",
                    "{Game}/perMod/3.dat",
                    "{Game}/perMod/4.dat",
                    "{Game}/perMod/5.dat",
                    "{Game}/perMod/6.dat",
                    "{Game}/perMod/7.dat",
                    "{Game}/perMod/8.dat",
                    // deletedFile: 9.dat is deleted
                    "{Game}/textures/a.dds",
                    "{Game}/bin/script.sh",
                    "{Game}/bin/binary",
                    "{Preferences}/preferences/settings.ini",
                    // newFile: newSave.dat is created
                    "{Saves}/saves/newSave.dat",
                    "{Saves}/saves/save1.dat",
                },
                "files have all been written to disk");

        diskState[modifiedFile].Item.Value.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        diskState[newFile].Item.Value.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");

    }
    
    [Fact]
    public async Task CanSwitchBetweenLoadouts()
    {
        var secondLoadout = await Game.Synchronizer.CreateLoadout(Install, "Second Loadout");
        await ApplyService.Synchronize(secondLoadout);
        FilesVerify(secondLoadout).Should().BeTrue();
        FilesVerify(BaseLoadout).Should().BeFalse();
        
        await ApplyService.Synchronize(BaseLoadout);
        FilesVerify(secondLoadout).Should().BeFalse();
        FilesVerify(BaseLoadout).Should().BeTrue();
        
        await ApplyService.Synchronize(secondLoadout);
        FilesVerify(secondLoadout).Should().BeTrue();
        FilesVerify(BaseLoadout).Should().BeFalse();
        
        bool FilesVerify(Loadout.ReadOnly loadout)
        {
            foreach(var file in loadout.Files.Where(f => f.Mod.Enabled))
            {
                var path = Install.LocationsRegister.GetResolvedPath(file.To);
                if (!path.FileExists)
                    return false;
            }
            return true;
        }
    }
    
    [Fact]
    public async Task ManageGame_SecondLoadout_UsesInitialDiskState()
    {
        // Arrange
        // Get our initial game disk state
        var initialLoadout = BaseLoadout;
        var initialDiskState = await Synchronizer.GetDiskState(initialLoadout.InstallationInstance);
        
        // Check that the files added by the first mod don't already exist, for sanity.
        var textureAbsPath = initialLoadout.InstallationInstance.LocationsRegister.GetResolvedPath(_texturePath);
        var meshAbsPath = initialLoadout.InstallationInstance.LocationsRegister.GetResolvedPath(_meshPath);
        textureAbsPath.FileExists.Should().BeFalse("The texture file should not exist in the second loadout");
        meshAbsPath.FileExists.Should().BeFalse("The mesh file should not exist in the second loadout");
        
        // Add a mod to the initial loadout
        await AddMod("TestMod",
            (_texturePath.Path, "texture.dds"),
            (_meshPath.Path, "mesh.nif"));

        // Apply the initial loadout
        await Synchronizer.Synchronize(initialLoadout);

        // Assert that the new files were deployed to disk after the first apply
        textureAbsPath.FileExists.Should().BeTrue("The texture file should exist after applying the initial loadout");
        meshAbsPath.FileExists.Should().BeTrue("The mesh file should exist after applying the initial loadout");

        // Act
        // Manage the game again to create a second loadout
        // This should reset our game folder to the initial state.
        var secondLoadout = await Synchronizer.CreateLoadout(Install, "Second Loadout");

        // Assert
        var secondLoadoutDiskState = await Synchronizer.GetDiskState(secondLoadout.InstallationInstance);

        // Check that the second loadout's initial disk state matches the original initial disk state
        secondLoadoutDiskState.Should().BeEquivalentTo(initialDiskState);

        // Check that the second loadout only contains the original game files
        var secondLoadoutFileTree = await Synchronizer.LoadoutToFlattenedLoadout(secondLoadout);
        secondLoadoutFileTree.GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .NotContain(_texturePath.ToString())
            .And
            .NotContain(_meshPath.ToString());

        // Check that the files added by the first loadout are not present in the second loadout
        textureAbsPath.FileExists.Should().BeFalse("The texture file should not exist in the second loadout");
        meshAbsPath.FileExists.Should().BeFalse("The mesh file should not exist in the second loadout");
    }
    
    [Fact]
    public async Task WhenDeletingLoadout_GameIsRevertedToInitialState()
    {
        // Arrange
        var initialLoadout = BaseLoadout;
        var initialDiskState = await Synchronizer.GetDiskState(initialLoadout.InstallationInstance);

        // Add a mod to the initial loadout
        await AddMod("TestMod",
            (_texturePath.Path, "texture.dds"),
            (_meshPath.Path, "mesh.nif"));

        await Synchronizer.Synchronize(initialLoadout);

        var textureAbsPath = initialLoadout.InstallationInstance.LocationsRegister.GetResolvedPath(_texturePath);
        var meshAbsPath = initialLoadout.InstallationInstance.LocationsRegister.GetResolvedPath(_meshPath);
        textureAbsPath.FileExists.Should().BeTrue("The texture file should exist after applying the initial loadout");
        meshAbsPath.FileExists.Should().BeTrue("The mesh file should exist after applying the initial loadout");

        // Act
        await Synchronizer.DeleteLoadout(initialLoadout.InstallationInstance, initialLoadout.LoadoutId);

        // Assert
        var model = initialLoadout.Rebase(Connection.Db);
        model.IsValid().Should().BeFalse("The loadout should be deleted from the database");

        var currentDiskState = await Synchronizer.GetDiskState(initialLoadout.InstallationInstance);
        currentDiskState.Should().BeEquivalentTo(initialDiskState, "The game should be reverted to the initial disk state");

        textureAbsPath.FileExists.Should().BeFalse("The texture file should not exist after deleting the loadout");
        meshAbsPath.FileExists.Should().BeFalse("The mesh file should not exist after deleting the loadout");
    }
}
