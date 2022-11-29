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
        var file = new ModFile(new GamePath(GameFolderType.Game, "foo/bar.pez"));
        file.Store.Should().NotBeNull();
        file.Id.Should().NotBeNull();

        _datastore.Get<ModFile>(file.Id).To.Should().BeEquivalentTo(file.To);
    }
}