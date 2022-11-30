using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class ModelTests
{
    private readonly IDataStore _datastore;

    public ModelTests(IDataStore store)
    {
        _datastore = store;
    }
    
    [Fact]
    public async Task CanCreateModFile()
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
        var list = new HashSet<Id>();

        var root = new Root<ListRegistry>(RootType.ModLists, _datastore);
        root.Alter(old => old with {Lists = old.Lists.With("My List", ModList.Empty)});

        using var _ = root.Changes.Select(x => x.New)
            .Subscribe(x => list.Add(x.Id));

        root.Alter( x => x with { Lists = x.Lists.With("My List 2", new ModList())});

        root.Value.Lists.Count.Should().Be(2);
        list.Should().Contain(root.Value.Id);
        
    }

    [Fact]
    public void CannotCreateEntityOutsideOfContext()
    {
        Action a = () => new FromArchive(To: new GamePath(GameFolderType.Game, "foo/bar.pez"),
            From: new HashRelativePath(Hash.Zero, RelativePath.Empty));
        
        a.Should().Throw<Exception>().WithMessage("Entity created outside of a IDataStore context");
    }
}