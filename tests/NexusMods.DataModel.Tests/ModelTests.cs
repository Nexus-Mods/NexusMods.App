using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
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
        var file = new ModFile(To:new GamePath(GameFolderType.Game, "foo/bar.pez"));
        file.Store.Should().NotBeNull();
        file.Id.Should().NotBeNull();

        _datastore.Get<ModFile>(file.Id).To.Should().BeEquivalentTo(file.To);
    }

    [Fact]
    public async Task CanSeeChangesViaObservable()
    {
        using var _2 = IDataStore.WithCurrent(_datastore);
        var list = new HashSet<Id>();
        
        var root = new Root<ModFile>(new ModFile(To: new GamePath(GameFolderType.Game, "foo/bar.pez")));
        using var _ = root.Changes.Select(x => x.New)
            .Subscribe(x => list.Add(x.Id));

        await root.AlterAsync(async x => x with { To = new GamePath(GameFolderType.Game, "foo/baz.pex") });

        root.Value.To.Should().NotBeEquivalentTo(new GamePath(GameFolderType.Game, "foo/bar.pez"));
        list.Should().Contain(root.Value.Id);
    }
}