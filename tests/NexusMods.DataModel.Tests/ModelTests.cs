using FluentAssertions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class ModelTests
{
    private readonly DataStore _datastore;

    public ModelTests(DataStore store)
    {
        _datastore = store;
    }
    
    [Fact]
    public async Task CanCreateModFile()
    {
        var file = new ModFile
        {
            To = new GamePath(GameFolderType.Game, "foo/bar.pez")
        };
        
        _datastore.StoreRoot(file).Should().Be(file.Id);
        file.IsDirty.Should().BeFalse();
        await _datastore.FlushChanges();
        
        var newRoot = _datastore.Load<ModFile>(file.Id);
        newRoot.IsDirty.Should().BeFalse();
        newRoot.Id.Should().Be(file.Id);
        
        file.To = new GamePath(GameFolderType.Preferences, "foo/bar.pez2");
        file.IsDirty.Should().BeTrue();
    }
}