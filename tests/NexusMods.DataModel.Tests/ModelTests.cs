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

    public ModelTests(IDataStore store, StubbedGame game, ModListManager manager)
    {
        _game = game;
        _install = game.Installations.First();
        _manager = manager;
        _datastore = store;
    }
    
    [Fact]
    public void CanCreateModFile()
    {
        using var _ = IDataStore.WithCurrent(_datastore);
        var file = new FromArchive(
            To:new GamePath(GameFolderType.Game, "foo/bar.pez"), 
            From: new HashRelativePath(new Hash(0), RelativePath.Empty));
        file.Store.Should().NotBeNull();
        file.Id.Should().NotBeNull();

        _datastore.Get<FromArchive>(file.Id).To.Should().BeEquivalentTo(file.To);
    }

    [Fact]
    public async Task CanSeeChangesViaObservable()
    {
        using var _2 = IDataStore.WithCurrent(_datastore);
        var list = new HashSet<string>();
        
        var modlist = await _manager.ManageGame(_install, "OldName");
        modlist.Changes.Subscribe(f => list.Add(f.Name));
        modlist.Alter(m => m with {Name = "NewName"});

        modlist.Value.Name.Should().Be("NewName");
        list.Count.Should().Be(1);
        list.First().Should().Be("NewName");

    }

    [Fact]
    public void CannotCreateEntityOutsideOfContext()
    {
        Action a = () => new FromArchive(To: new GamePath(GameFolderType.Game, "foo/bar.pez"),
            From: new HashRelativePath(Hash.Zero, RelativePath.Empty));
        
        a.Should().Throw<Exception>().WithMessage("Entity created outside of a IDataStore context");
    }
}