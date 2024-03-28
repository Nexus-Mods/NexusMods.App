using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using ModFileId = NexusMods.Abstractions.Loadouts.Mods.ModFileId;

namespace NexusMods.DataModel.Tests;

public class ALoadoutSynchronizerTests : ADataModelTest<ALoadoutSynchronizerTests>
{
    private readonly IStandardizedLoadoutSynchronizer _synchronizer;
    private readonly Dictionary<ModId, string> _modNames = new();
    private readonly Dictionary<string, ModId> _modIdForName = new();
    private readonly Dictionary<ModFileId, ModFilePair> _pairs = new();
    private readonly List<ModId> _modIds = new();
    private const int ModCount = 10;

    private static GamePath _texturePath = new(LocationId.Game, "textures/a.dds");
    private static GamePath _meshPath = new(LocationId.Game, "meshes/b.nif");
    private static GamePath _prefsPath = new(LocationId.Preferences, "preferences/prefs.dat");
    private static GamePath _savePath = new(LocationId.Saves, "saves/save.dat");

    private static GamePath[] _allPaths = {_texturePath , _meshPath, _prefsPath, _savePath};
    private Loadout _originalLoadout;

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

        _originalLoadout = BaseList.Value;

        BaseList.Alter("Remove Base Mods",
            l => l with { Mods = l.Mods.Keep(m => m with { Enabled = false }) });


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


        for (var i = 0; i < ModCount - 1; i++)
        {
            LoadoutRegistry.Alter(BaseList.Id, _modIds[i], "Sort rule for mod {i}", mod =>
                mod with { SortRules = mod.SortRules.Add(new After<Mod, ModId> { Other = _modIds[i + 1]})});
        }

        foreach (var modId in _modIds)
        {
            var mod = BaseList.Value.Mods[modId];

            foreach (var (fileId, file) in mod.Files)
            {
                _pairs[fileId] = new ModFilePair { Mod = mod, File = file };
            }
        }
    }

    [Fact]
    public async Task ApplyingTwiceDoesNothing()
    {
        // If apply is buggy, it will result in a "needs ingest" error when we try to re-apply. Because Apply
        // will have not properly updated the disk state, and it will error because the disk state is not in sync
        await _synchronizer.Apply(BaseList.Value);
        await _synchronizer.Apply(BaseList.Value);

        // This should not throw as the disk state should be in sync
        BaseList.Alter("Changing the name", l => l with { Name = "Changed Name"});
        await _synchronizer.Apply(BaseList.Value);
    }

    [Fact]
    public async Task CanFlattenLoadout()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);

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
        var topFiles = BaseList.Value.Mods[topMod].Files.Values.OfType<StoredFile>().ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            flattened[path].Item.Value!.File.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseList.Value.Mods[_modIds[i]].Files.Values.OfType<StoredFile>().First(f => f.To == path);
            flattened[path].Item.Value!.File.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    [Fact]
    public async Task CanCreateFileTree()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseList.Value);

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
        var topFiles = BaseList.Value.Mods[topMod].Files.Values.OfType<StoredFile>().ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            fileTree[path].Item.Value!.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseList.Value.Mods[_modIds[i]].Files.Values.OfType<StoredFile>().First(f => f.To == path);
            fileTree[path].Item.Value!.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    [Fact]
    public async Task CanWriteDiskTreeToDisk()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseList.Value);
        var prevState = DiskStateRegistry.GetState(BaseList.Value.Installation)!;
        var diskState = await _synchronizer.FileTreeToDisk(fileTree, BaseList.Value, flattened, prevState, Install);

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
    public async Task CanIngestDiskState()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseList.Value);

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
        await _synchronizer.Apply(BaseList.Value);

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
        var prevFlattenedLoadout = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var prevFileTree = await _synchronizer.FlattenedLoadoutToFileTree(prevFlattenedLoadout, BaseList.Value);
        var prevDiskState = DiskStateRegistry.GetState(BaseList.Value.Installation);

        var fileTree = await _synchronizer.DiskToFileTree(diskState, BaseList.Value, prevFileTree, prevDiskState);

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

        ((StoredFile) fileTree[modifiedFile].Item.Value!).Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        ((StoredFile) fileTree[newFile].Item.Value!).Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");

        fileTree[deletedFile].Should().BeNull("the file should have been deleted");

    }


    [Fact]
    public async Task CanIngestFlattenedList()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseList.Value);

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
        var prevFlattenedLoadout = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var prevFileTree = await _synchronizer.FlattenedLoadoutToFileTree(prevFlattenedLoadout, BaseList.Value);
        var prevDiskState = DiskStateRegistry.GetState(BaseList.Value.Installation)!;

        var fileTree = await _synchronizer.DiskToFileTree(diskState, BaseList.Value, prevFileTree, prevDiskState);
        var flattenedLoadout = await _synchronizer.FileTreeToFlattenedLoadout(fileTree, BaseList.Value, prevFlattenedLoadout);

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

        var flattenedModifiedPair = flattenedLoadout[modifiedFile].Item.Value!;
        var flattenedModifiedFile = (StoredFile)flattenedModifiedPair.File;
        flattenedModifiedFile.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        flattenedModifiedPair.Mod.Should().Be(prevFlattenedLoadout[modifiedFile].Item.Value!.Mod, "the mod should be the same");

        var flattenedNewPair = flattenedLoadout[newFile].Item.Value!;
        var flattenedNewFile = (StoredFile) flattenedNewPair.File;
        flattenedNewFile.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");
        var newMod = flattenedNewPair.Mod;
        newMod.ModCategory.Should().Be(Mod.SavesCategory, "the mod should be in the overrides category");
        newMod.Name.Should().Be("Saved Games", "the mod should be named after the file");

        flattenedLoadout[deletedFile].Should().BeNull("the file should have been deleted");
    }


    [Fact]
    public async Task CanIngestLoadout()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseList.Value);

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
        var prevFlattenedLoadout = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var prevFileTree = await _synchronizer.FlattenedLoadoutToFileTree(prevFlattenedLoadout, BaseList.Value);
        var prevDiskState = DiskStateRegistry.GetState(BaseList.Value.Installation)!;

        var fileTree = await _synchronizer.DiskToFileTree(diskState, BaseList.Value, prevFileTree, prevDiskState);
        var flattenedLoadout = await _synchronizer.FileTreeToFlattenedLoadout(fileTree, BaseList.Value, prevFlattenedLoadout);
        var loadout = await _synchronizer.FlattenedLoadoutToLoadout(flattenedLoadout, BaseList.Value, prevFlattenedLoadout);

        var flattenedAgain = await _synchronizer.LoadoutToFlattenedLoadout(loadout);

        flattenedAgain.GetAllDescendentFiles()
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

        var flattenedModifiedPair = flattenedAgain[modifiedFile].Item.Value!;
        var flattenedModifiedFile = (StoredFile)flattenedModifiedPair.File;
        flattenedModifiedFile.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        flattenedModifiedPair.Mod.Id.Should().Be(prevFlattenedLoadout[modifiedFile].Item.Value!.Mod.Id, "the mod should be the same");

        var flattenedNewPair = flattenedAgain[newFile].Item.Value!;
        var flattenedNewFile = (StoredFile) flattenedNewPair.File;
        flattenedNewFile.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");
        var newMod = flattenedNewPair.Mod;
        newMod.ModCategory.Should().Be(Mod.SavesCategory, "the mod should be in the overrides category");
        newMod.Name.Should().Be("Saved Games", "the mod should be named after the file");

        flattenedAgain[deletedFile].Should().BeNull("the file should have been deleted");

        await _synchronizer.BackupNewFiles(loadout.Installation, fileTree);

        (await FileStore.HaveFile(new byte[] { 0x04, 0x05, 0x06 }.XxHash64()))
            .Should().BeTrue("the file should have been backed up");
        (await FileStore.HaveFile(new byte[] { 0x01, 0x02, 0x03 }.XxHash64()))
            .Should().BeTrue("the file should have been backed up");
    }

    [Fact]
    public async Task CanMergeLoadouts()
    {
        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(modifiedFile).Parent.CreateDirectory();
        Install.LocationsRegister.GetResolvedPath(newFile).Parent.CreateDirectory();

        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x04, 0x05, 0x06 });

        // Represents the loadout the user has changed but not applied
        var userModified = BaseList.Value;

        // Represents the loadout from the ingest process
        var ingested = await _synchronizer.Ingest(_originalLoadout);

        var result = _synchronizer.MergeLoadouts(userModified, ingested);

        // Re-enable the game files as they are disabled by the merged loadout
        var gameFilesId = result.Mods.First(m => m.Value.ModCategory == Mod.GameFilesCategory).Key;
        result = result.Alter(gameFilesId, m => m with { Enabled = true });


        // The merged loadout should contain all the mods from both loadouts
        userModified.Mods
            .Select(m => (m.Key, m.Value.Name))
            .Concat(ingested.Mods.Select(m => (m.Key, m.Value.Name)))
            .Distinct()
            .Should()
            .BeEquivalentTo(result.Mods.Select(m => (m.Key, m.Value.Name)),
                "the merged loadout should contain all the mods from both loadouts");

        // The game files should be enabled
        result.Mods
            .First(m => m.Value.ModCategory == Mod.GameFilesCategory)
            .Value.Enabled.Should().BeTrue("the game files should be enabled");

        // The merged loadout should contain all the files from both loadouts
        var flattenedUserModified = await _synchronizer.LoadoutToFlattenedLoadout(userModified);
        var flattenedIngested = await _synchronizer.LoadoutToFlattenedLoadout(ingested);
        var flattenedResult = await _synchronizer.LoadoutToFlattenedLoadout(result);

        flattenedUserModified.GetAllDescendentFiles()
            .Concat(flattenedIngested.GetAllDescendentFiles())
            .Select(f => f.GamePath().ToString())
            .Distinct()
            .Should()
            .BeEquivalentTo(flattenedResult.GetAllDescendentFiles().Select(f => f.GamePath().ToString()),
                "the merged loadout should contain all the files from both loadouts");
    }


    [JsonName("GeneratedTestFile")]
    record GeneratedTestFile : AModFile, IGeneratedFile, IToFile
    {
        public GamePath To => new(LocationId.Game, "generated.txt");

        public required Char[] Data { get; init; }
        public async ValueTask<Hash?> Write(Stream stream, Loadout loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree)
        {
            var bytes = Data.Select(c => (byte)c).ToArray();
            await stream.WriteAsync(bytes, 0, bytes.Length);
            return bytes.XxHash64();
        }

        public async ValueTask<AModFile> Update(DiskStateEntry newEntry, Stream stream)
        {
            return this with { Data = (await stream.ReadAllTextAsync()).ToArray() };
        }
    }

    [Fact]
    public async Task CanWriteGeneratedFiles()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseList.Value);

        var modId = ModId.NewId();
        var fileId = ModFileId.NewId();

        var generatedFile = new GeneratedTestFile()
        {
            Id = fileId,
            Data = new[] { 'A', 'B', 'C' }
        };


        BaseList.Alter("Add generated file", l => l with
        {
            Mods = l.Mods.With(modId, new Mod
            {
                Id = modId,
                Name = "Generated Files",
                Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore)
                    .With(fileId, generatedFile),
                Enabled = true
            })
        });

        var outputPath = Install.LocationsRegister.GetResolvedPath(generatedFile.To);
        var state = await _synchronizer.Apply(BaseList.Value);

        state[generatedFile.To].Item.Value.Hash.Should().Be("ABC".XxHash64AsUtf8(), "the file should have been generated");

        outputPath.FileExists.Should().BeTrue("the file should have been written to disk");
        (await outputPath.ReadAllTextAsync()).Should().Be("ABC", "the file should contain the generated data");

        // So the file is generated now, but what if we change the data?

        LoadoutRegistry.Alter<GeneratedTestFile>(BaseList.Value.LoadoutId, modId, fileId, "Change the data", f => f with
        {
            Data = new[] { 'D', 'E', 'F' }
        });

        var newState = await _synchronizer.Apply(BaseList.Value);

        newState[generatedFile.To].Item.Value.Hash.Should().Be("DEF".XxHash64AsUtf8(), "the file should have been generated");
        outputPath.FileExists.Should().BeTrue("the file should still exist");
        (await outputPath.ReadAllTextAsync()).Should().Be("DEF", "the file should contain the new generated data");

        // Now that we've changed the data, what if we change the disk state?

        await outputPath.WriteAllTextAsync("DEADBEEF");

        var newLoadout = await _synchronizer.Ingest(BaseList.Value);

        newLoadout.Mods[modId].Files.ContainsKey(fileId).Should().BeTrue("The file should still exist");

        var generatedFileUpdated = (GeneratedTestFile)newLoadout.Mods[modId].Files[fileId];
        generatedFileUpdated.Data.Should().BeEquivalentTo(new[] { 'D', 'E', 'A', 'D', 'B', 'E', 'E', 'F' }, "the data should be updated");

        // Delete the file
        outputPath.Delete();

        newLoadout = await _synchronizer.Ingest(newLoadout);
        newLoadout.Mods[modId].Files.ContainsKey(fileId).Should().BeFalse("The file should no longer exist");

    }

    [Fact]
    public async Task CanSwitchBetweenLoadouts()
    {
        var listA = BaseList;
        // Apply the old state
        var baseState = await _synchronizer.Apply(listA.Value);

        var newId = LoadoutId.NewId();
        LoadoutRegistry.Alter(newId, "Clone List", 
            _ => listA.Value with { 
                LoadoutId = newId, 
                Name = "List B", 
            });
        var listB = LoadoutRegistry.GetMarker(newId);
        
        // We have two lists, so we can now swap between them and that's fine because they are the same so far
        await _synchronizer.Apply(listB.Value);
        await _synchronizer.Apply(listA.Value);
        await _synchronizer.Apply(listB.Value);

        GamePath testFilePath = new(LocationId.Game, "textures/test_file_switcher.dds");
        
        // Now let's add a mod to listA
        await AddMod("Other Files",
            // Each mod overrides the same files for these three files
            (testFilePath.Path, $"test-file-switcher")
        );
        
        listA.Value.Mods.Count.Should().Be(listB.Value.Mods.Count + 1, "the mod should have been added");

        var absPath = Install.LocationsRegister.GetResolvedPath(testFilePath);
        // And apply it
        await _synchronizer.Apply(listA.Value);
        absPath.FileExists.Should().BeTrue("the file should have been written to disk");
        (await absPath.ReadAllTextAsync()).Should().Be("test-file-switcher", "the file should contain the new data");
        
        
        // And switch back to listB
        await _synchronizer.Apply(listB.Value);
        
        absPath.FileExists.Should().BeFalse("the file should have been removed from disk");
        
        
        var listCValue = await _synchronizer.Manage(Install);
        
        var listC = LoadoutRegistry.GetMarker(listCValue.LoadoutId);
        await _synchronizer.Ingest(listC.Value);

        await _synchronizer.Apply(listC.Value);
        
        await _synchronizer.Apply(listA.Value);
        await _synchronizer.Apply(listB.Value);
        await _synchronizer.Apply(listC.Value);




    }
}
