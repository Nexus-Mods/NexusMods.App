using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class RocksDBDataStoreTests : ADataModelTest<RocksDBDataStoreTests>
{
    public RocksDBDataStoreTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public void CanGetAndSetHashedValues()
    {
        var foo = new FromArchive
        {
            Store = DataStore,
            Hash = Hash.Zero,
            From = new HashRelativePath((Hash)42L, Array.Empty<RelativePath>()),
            Size = (Size)42L,
            To = new GamePath(GameFolderType.Game, "test.foo")
        };
        foo.Id.ToString().Should().NotBeEmpty();
        DataStore.Get<FromArchive>(foo.Id).Should().NotBeNull();
    }

    [Fact]
    public void CanPutAndGetRoots()
    {
        var id = new Id64(EntityCategory.ModLists, 42L);
        DataStore.GetRoot(RootType.Tests).Should().BeNull();

        DataStore.PutRoot(RootType.Tests, IdEmpty.Empty, id).Should().BeTrue();
        DataStore.GetRoot(RootType.Tests).Should().Be(id);

    }


}