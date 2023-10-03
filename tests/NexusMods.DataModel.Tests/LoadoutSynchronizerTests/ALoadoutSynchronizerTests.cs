using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using ModId = NexusMods.DataModel.Loadouts.ModId;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class ALoadoutSynchronizerTests : ADataModelTest<LoadoutSynchronizerStub>
{
    private readonly LoadoutSynchronizerStub _synchronizer;
    private readonly Dictionary<ModId, string> _modNames = new();
    private readonly Dictionary<string, ModId> _modIdForName = new();
    private readonly Dictionary<ModFileId, ModFilePair> _pairs = new();
    private readonly List<ModId> _modIds = new();
    private const int ModCount = 10;

    private static GamePath _texturePath = new(LocationId.Game, "textures/a.dds");
    private static GamePath _meshPath = new(LocationId.Game, "meshes/b.nif");
    private static GamePath _prefsPath = new(LocationId.Preferences, "preferences/prefs.dat");
    private static GamePath _savePath = new(LocationId.Saves, "saves/save.dat");

    private static GamePath[] _allPaths = new [] {_texturePath , _meshPath, _prefsPath, _savePath};

    public ALoadoutSynchronizerTests(IServiceProvider provider) : base(provider)
    {
        _synchronizer = LoadoutSynchronizerStub.Create(provider);
        ((StubbedGame)Game).SetSynchronizer(_synchronizer);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

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
                ($"/perMod/{i}.dat", "mod{i}-perMod"));



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
    public async Task CanFlattenLoadout()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);

        // Game files are not included, because they are disabled in the initializer
        flattened.GetAllDescendentFiles()
            .Select(f => f.Path.ToString())
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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "all the mods are flattened into a single tree, with overlaps removed");

        var topMod = _modIds[0];
        var topFiles = BaseList.Value.Mods[topMod].Files.Values.OfType<FromArchive>().ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            flattened[path].Value!.File.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseList.Value.Mods[_modIds[i]].Files.Values.OfType<FromArchive>().First(f => f.To == path);
            flattened[path].Value!.File.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    [Fact]
    public async Task CanCreateFileTree()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseList.Value);

        fileTree.GetAllDescendentFiles()
            .Select(f => f.Path.ToString())
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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "all the mods are flattened into a single tree, with overlaps removed");

        var topMod = _modIds[0];
        var topFiles = BaseList.Value.Mods[topMod].Files.Values.OfType<FromArchive>().ToDictionary(d => d.To);

        foreach (var path in _allPaths)
        {
            fileTree[path].Value!.Should()
                .BeEquivalentTo(topFiles[path], "the top mod should be the one that contributes the file data");
        }

        for (var i = 0; i < ModCount; i++)
        {
            var path = new GamePath(LocationId.Game, $"perMod/{i}.dat");
            var originalFile = BaseList.Value.Mods[_modIds[i]].Files.Values.OfType<FromArchive>().First(f => f.To == path);
            fileTree[path].Value!.Should()
                .BeEquivalentTo(originalFile, "these files have unique paths, so they should not be overridden");
        }
    }

    [Fact]
    public async Task CanWriteDiskTreeToDisk()
    {
        var flattened = await _synchronizer.LoadoutToFlattenedLoadout(BaseList.Value);
        var fileTree = await _synchronizer.FlattenedLoadoutToFileTree(flattened, BaseList.Value);
        var prevState = DiskStateRegistry.GetState(BaseList.Id)!;
        var diskState = await _synchronizer.FileTreeToDisk(fileTree, prevState, Install);

        diskState.GetAllDescendentFiles()
            .Select(f => f.Path.ToString())
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
                    "{Preferences}/preferences/prefs.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        foreach (var file in diskState.GetAllDescendentFiles())
        {
            var path = Install.LocationsRegister.GetResolvedPath(file.Path);
            path.FileExists.Should().BeTrue("the file should exist on disk");
            path.FileInfo.Size.Should().Be(file.Value!.Size, "the file size should match");
            path.FileInfo.LastWriteTimeUtc.Should()
                .Be(file.Value.LastModified, "the file last modified time should match");
            (await path.XxHash64Async()).Should().Be(file.Value.Hash, "the file hash should match");
        }
    }

    [Fact]
    public async Task CanIngestFileTree()
    {
        // Apply the old state
        await _synchronizer.Apply(BaseList.Value);

        // Setup some paths
        var modifiedFile = new GamePath(LocationId.Game, "meshes/b.nif");
        var newFile = new GamePath(LocationId.Saves, "saves/newSave.dat");
        var deletedFile = new GamePath(LocationId.Game, "/perMod/9.dat");

        // Modify the files on disk
        Install.LocationsRegister.GetResolvedPath(deletedFile).Delete();
        await Install.LocationsRegister.GetResolvedPath(modifiedFile).WriteAllBytesAsync(new byte[] { 0x01, 0x02, 0x03 });
        await Install.LocationsRegister.GetResolvedPath(newFile).WriteAllBytesAsync(new byte[] { 0x04, 0x05, 0x06 });

        var diskState = await _synchronizer.GetDiskState(Install);

        diskState.GetAllDescendentFiles()
            .Select(f => f.Path.ToString())
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
                    "{Preferences}/preferences/prefs.dat",
                    // newFile: newSave.dat is created
                    "{Saves}/saves/newSave.dat",
                    "{Saves}/saves/save.dat"
                },
                "files have all been written to disk");

        diskState[modifiedFile].Value!.Hash.Should().Be(new byte[] { 0x01, 0x02, 0x03 }.XxHash64(), "the file should have been modified");
        diskState[newFile].Value!.Hash.Should().Be(new byte[] { 0x04, 0x05, 0x06 }.XxHash64(), "the file should have been created");

    }
}
