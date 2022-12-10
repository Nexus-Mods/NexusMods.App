using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class RocksDBDataStoreTests
{
    private readonly TemporaryFileManager _manager;
    private readonly RocksDbDatastore _dataStore;
    private readonly TemporaryPath _tempPath;

    public RocksDBDataStoreTests(IServiceProvider provider, TemporaryFileManager manager)
    {
        _manager = manager;
        _tempPath = manager.CreateFolder();
        _dataStore = new RocksDbDatastore(_tempPath, provider);
    }

    [Fact]
    public void CanGetAndSetHashedValues()
    {
        var foo = new FromArchive()
        {
            Store = _dataStore,
            Hash = Hash.Zero,
            From = new HashRelativePath((Hash)42L, Array.Empty<RelativePath>()),
            Size = (Size)42L,
            To = new GamePath(GameFolderType.Game, "test.foo")
        };
        foo.Id.ToString().Should().Be("ModLists-1F87BA45BCC1FFD3");
        _dataStore.Get<FromArchive>(foo.Id).Should().NotBeNull();
    }

    [Fact]
    public void CanPutAndGetRoots()
    {
        var id = new Id64(EntityCategory.ModLists, 42L);
        _dataStore.GetRoot(RootType.ModLists).Should().BeNull();

        _dataStore.PutRoot(RootType.ModLists, IdEmpty.Empty, id).Should().BeTrue();
        _dataStore.GetRoot(RootType.ModLists).Should().Be(id);

    }
}