using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.DataModel.Tests;

public class ALoadoutSynchronizerTests : ADataModelTest<ALoadoutSynchronizerTests>
{
    protected readonly IStandardizedLoadoutSynchronizer _synchronizer;
    private readonly Dictionary<ModId, string> _modNames = new();
    private readonly Dictionary<string, ModId> _modIdForName = new();
    private readonly Dictionary<FileId, File.Model> _pairs = new();
    private readonly List<ModId> _modIds = new();
    private const int ModCount = 10;

    private static GamePath _texturePath = new(LocationId.Game, "textures/a.dds");
    private static GamePath _meshPath = new(LocationId.Game, "meshes/b.nif");
    private static GamePath _prefsPath = new(LocationId.Preferences, "preferences/prefs.dat");
    private static GamePath _savePath = new(LocationId.Saves, "saves/save.dat");
    private static GamePath _configPath = new(LocationId.Game, "config.ini");
    private static GamePath _imagePath = new(LocationId.Game, "Data/image.dds");

    private static GamePath[] _allPaths = {_texturePath , _meshPath, _prefsPath, _savePath};
    //private Loadout _originalLoadout;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    public ALoadoutSynchronizerTests(IServiceProvider provider) : base(provider)
    {
        _synchronizer = (IStandardizedLoadoutSynchronizer)Game.Synchronizer;
        
    }

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
        // If apply is buggy, it will result in a "needs ingest" error when we try to re-apply. Because Apply
        // will have not properly updated the disk state, and it will error because the disk state is not in sync
        await _synchronizer.Apply(BaseLoadout);
        await _synchronizer.Apply(BaseLoadout);

        // This should not throw as the disk state should be in sync

        using var tx = Connection.BeginTransaction();
        tx.Add(BaseLoadout.Id, Loadout.Name, "Changed Name");
        await tx.Commit();
        Refresh(ref BaseLoadout);
        
        await _synchronizer.Apply(BaseLoadout);
    }

    
    [Fact]
    public async Task ApplyingDeletesCleansUpEmptyDirectories()
    {
        await _synchronizer.Apply(BaseLoadout);

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
        await _synchronizer.Apply(BaseLoadout);

        path1.FileExists.Should().BeTrue("the file should exist");

        // Delete the mod
        {
            using var tx = Connection.BeginTransaction();
            tx.Add(toDelete.Value, Mod.Enabled, false);
            await tx.Commit();
        }

        Refresh(ref BaseLoadout);
        await _synchronizer.Apply(BaseLoadout);

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
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);

        // Game files are not included, because they are disabled in the initializer
        flattened.GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .BeEquivalentTo(new []
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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "all the mods are flattened into a single tree, with overlaps removed");

        var topMod = _modIds[0];
        var topFiles = Connection.Db.Get<Mod.Model>(topMod.Value).Files.ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            flattened[path].Item.Value.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseLoadout.Files.First(f => f.To == path);
            flattened[path].Item.Value.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    
    [Fact]
    public async Task CanCreateFileTree()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseLoadout);

        fileTree.GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .BeEquivalentTo(new []
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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "all the mods are flattened into a single tree, with overlaps removed");

        var topMod = _modIds[0];
        var topFiles = BaseLoadout.Mods.First(m => m.Id == topMod).Files.ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            fileTree[path].Item.Value!.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseLoadout.Mods.First(m => m.Id == _modIds[i]).Files.First(f => f.To == path);
            fileTree[path].Item.Value!.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    
    [Fact]
    public async Task CanWriteDiskTreeToDisk()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseLoadout);
        var prevState = DiskStateRegistry.GetState(BaseLoadout.Installation)!;
        var diskState = await _synchronizer.FileTreeToDisk(fileTree, BaseLoadout, flattened, prevState, Install);

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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        foreach (var file in diskState.GetAllDescendentFiles())
        {
            var path = Install.LocationsRegister.GetResolvedPath(file.GamePath());
            path.FileExists.Should().BeTrue("the file should exist on disk");
            path.FileInfo.Size.Should().Be(file.Item.Value.Size, "the file size should match");
            path.FileInfo.LastWriteTimeUtc.Should()
                .Be(file.Item.Value.LastModified, "the file last modified time should match");
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
        var prevDiskState = DiskStateRegistry.GetState(BaseLoadout.Installation)!;

        await AddMod("ReplacingConfigMod",
            // This replaces the config file without changing the contents
            (_configPath.Path, "config.ini"),
            // This replaces the image file with a new one
            (_imagePath.Path, "modifiedImage.dds")
            );

        Refresh(ref BaseLoadout);
        var diffTree = await _synchronizer.LoadoutToDiskDiff(BaseLoadout, prevDiskState );
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
        await _synchronizer.Apply(BaseLoadout);

        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");
        var deletedFile = new GamePath(LocationId.Game, "perMod/9.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x04, 0x05, 0x06 });

        var diskState = await _synchronizer.GetDiskState(Install);

        diskState.GetAllDescendentFiles()
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
                    "{Preferences}/preferences/prefs.dat",
                    // newFile: newSave.dat is created
                    "{Saves}/saves/newSave.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        diskState[modifiedFile].Item.Value.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        diskState[newFile].Item.Value.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");

    }

    
    [Fact]
    public async Task CanIngestFileTree()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseLoadout);

        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");
        var deletedFile = new GamePath(LocationId.Game, "perMod/9.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x04, 0x05, 0x06 });

        var diskState = await _synchronizer.GetDiskState(Install);

        // Reconstruct the previous file tree
        var prevFlattenedLoadout = await _synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var prevFileTree = await _synchronizer.FlattenedLoadoutToFileTree(prevFlattenedLoadout, BaseLoadout);
        var prevDiskState = DiskStateRegistry.GetState(BaseLoadout.Installation);

        var fileTree = await _synchronizer.DiskToFileTree(diskState, BaseLoadout, prevFileTree, prevDiskState);

        fileTree.GetAllDescendentFiles()
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
                    "{Game}/bin/script.sh",
                    "{Game}/bin/binary",
                    // deletedFile: 9.dat is deleted
                    "{Game}/textures/a.dds",
                    "{Preferences}/preferences/prefs.dat",
                    // newFile: newSave.dat is created
                    "{Saves}/saves/newSave.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        fileTree[modifiedFile].Item.Value.As<StoredFile.Model>().Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        fileTree[newFile].Item.Value.As<StoredFile.Model>().Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");

        fileTree[deletedFile].Should().BeNull("the file should have been deleted");

    }
    
    [Fact]
    public async Task CanIngestFlattenedList()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseLoadout);

        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");
        var deletedFile = new GamePath(LocationId.Game, "perMod/9.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x04, 0x05, 0x06 });

        var diskState = await _synchronizer.GetDiskState(Install);

        // Reconstruct the previous file tree
        var prevFlattenedLoadout = await _synchronizer.LoadoutToFlattenedLoadout(BaseLoadout);
        var prevFileTree = await _synchronizer.FlattenedLoadoutToFileTree(prevFlattenedLoadout, BaseLoadout);
        var prevDiskState = DiskStateRegistry.GetState(BaseLoadout.Installation)!;

        var fileTree = await _synchronizer.DiskToFileTree(diskState, BaseLoadout, prevFileTree, prevDiskState);
        var flattenedLoadout = await _synchronizer.FileTreeToFlattenedLoadout(fileTree, BaseLoadout, prevFlattenedLoadout);

        flattenedLoadout.GetAllDescendentFiles()
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
                    "{Game}/bin/script.sh",
                    "{Game}/bin/binary",
                    // deletedFile: 9.dat is deleted
                    "{Game}/textures/a.dds",
                    "{Preferences}/preferences/prefs.dat",
                    // newFile: newSave.dat is created
                    "{Saves}/saves/newSave.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        var flattenedModifiedPair = flattenedLoadout[modifiedFile].Item.Value;
        var flattenedModifiedFile = flattenedModifiedPair.Remap<StoredFile.Model>();
        flattenedModifiedFile.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");

        var flattenedNewPair = flattenedLoadout[newFile].Item.Value;
        var flattenedNewFile = flattenedNewPair.Remap<StoredFile.Model>();
        flattenedNewFile.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");
        var newMod = flattenedNewPair.Mod;
        newMod.Category.Should().Be(ModCategory.Overrides, "the mod should be in the overrides category");
        newMod.Name.Should().Be("Overrides", "the mod is the overrides mod");

        flattenedLoadout[deletedFile].Should().BeNull("the file should have been deleted");
    }
    
    [Fact]
    public async Task CanSwitchBetweenLoadouts()
    {
        var secondLoadout = await Game.Synchronizer.Manage(Install, "Second Loadout");
        await ApplyService.Apply(secondLoadout);
        FilesVerify(secondLoadout).Should().BeTrue();
        FilesVerify(BaseLoadout).Should().BeFalse();
        
        await ApplyService.Apply(BaseLoadout);
        FilesVerify(secondLoadout).Should().BeFalse();
        FilesVerify(BaseLoadout).Should().BeTrue();
        
        await ApplyService.Apply(secondLoadout);
        FilesVerify(secondLoadout).Should().BeTrue();
        FilesVerify(BaseLoadout).Should().BeFalse();
        
        bool FilesVerify(Loadout.Model loadout)
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
        var initialDiskState = await _synchronizer.GetDiskState(initialLoadout.Installation);

        // Add a mod to the initial loadout
        await AddMod("TestMod",
            (_texturePath.Path, "texture.dds"),
            (_meshPath.Path, "mesh.nif"));

        // Apply the initial loadout
        await _synchronizer.Apply(initialLoadout);

        // Assert that the new files were deployed to disk after the first apply
        var textureAbsPath = initialLoadout.Installation.LocationsRegister.GetResolvedPath(_texturePath);
        var meshAbsPath = initialLoadout.Installation.LocationsRegister.GetResolvedPath(_meshPath);
        textureAbsPath.FileExists.Should().BeTrue("The texture file should exist after applying the initial loadout");
        meshAbsPath.FileExists.Should().BeTrue("The mesh file should exist after applying the initial loadout");

        // Act
        // Manage the game again to create a second loadout
        // This should reset our game folder to the initial state.
        var secondLoadout = await Game.Synchronizer.Manage(Install, "Second Loadout");

        // Assert
        var secondLoadoutDiskState = await _synchronizer.GetDiskState(secondLoadout.Installation);

        // Check that the second loadout's initial disk state matches the original initial disk state
        secondLoadoutDiskState.Should().BeEquivalentTo(initialDiskState);

        // Check that the second loadout only contains the original game files
        var secondLoadoutFileTree = await _synchronizer.LoadoutToFlattenedLoadout(secondLoadout);
        secondLoadoutFileTree.GetAllDescendentFiles()
            .Select(f => f.GamePath().ToString())
            .Should()
            .NotContain(_texturePath.ToString())
            .And
            .NotContain(_meshPath.ToString());
    }
}
