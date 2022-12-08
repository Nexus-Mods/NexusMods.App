using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.Tests;

namespace NexusMods.DataModel.Tests;

public class ModelTests
{
    private readonly IDataStore _datastore;
    private readonly StubbedGame _game;
    private readonly GameInstallation _install;
    private readonly ModListManager _manager;
    private readonly TemporaryFileManager _temporaryFileManager;

    public ModelTests(IDataStore store, StubbedGame game, ModListManager manager, TemporaryFileManager temporaryFileManager)
    {
        _game = game;
        _install = game.Installations.First();
        _manager = manager;
        _datastore = store;
        _temporaryFileManager = temporaryFileManager;
    }
    
    [Fact]
    public void CanCreateModFile()
    {
        var file = new FromArchive
        {
            To = new GamePath(GameFolderType.Game, "foo/bar.pez"),
            From = new HashRelativePath(new Hash(0), RelativePath.Empty),
            Hash = (Hash)0x42L,
            Size = 44L,
            Store = _datastore
        };
        file.Store.Should().NotBeNull();
        file.Id.Should().NotBeNull();

        _datastore.Get<FromArchive>(file.Id).To.Should().BeEquivalentTo(file.To);
    }

    [Fact]
    public async Task CanSeeChangesViaObservable()
    {
        var list = new HashSet<string>();
        
        var modlist = await _manager.ManageGame(_install, "OldName");
        modlist.Changes.Subscribe(f => list.Add(f.Name));
        modlist.Alter(m => m with {Name = "NewName"});

        modlist.Value.Name.Should().Be("NewName");
        list.Count.Should().Be(1);
        list.First().Should().Be("NewName");
    }
    
    [Fact]
    public async Task CanInstallAMod()
    {
        var mod1 = KnownFolders.EntryFolder.Combine("Resources/data_7zip_lzma2.7z");
        var mod2 = KnownFolders.EntryFolder.Combine("Resources/data_zip_lzma.zip");
        
        var name = Guid.NewGuid().ToString();
        var modlist = await _manager.ManageGame(_install, name);
        await modlist.Install(mod1, "Mod1", CancellationToken.None);
        await modlist.Install(mod2, "", CancellationToken.None);

        modlist.Value.Mods.Count.Should().Be(3);
        modlist.Value.Mods.Sum(m => m.Files.Count).Should().Be(10);


    }
}