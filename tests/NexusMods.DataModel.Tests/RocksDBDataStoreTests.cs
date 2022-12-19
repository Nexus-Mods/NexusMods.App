using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
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
        var id = new Id64(EntityCategory.Loadouts, 42L);
        DataStore.GetRoot(RootType.Tests).Should().BeNull();

        DataStore.PutRoot(RootType.Tests, IdEmpty.Empty, id).Should().BeTrue();
        DataStore.GetRoot(RootType.Tests).Should().Be(id);
    }

    [Fact]
    public void CanStoreLargeEntitiesInDB()
    {
        var files = Enumerable.Range(0, 1024).Select(idx => new FromArchive
        {
            From = new HashRelativePath(idx, $"{idx}.file".ToRelativePath()),
            Hash = idx,
            Size = idx,
            Store = DataStore,
            To = new GamePath(GameFolderType.Game, $"{idx}.file"),
        }).ToList();
        
        var set = new EntityHashSet<AModFile>(DataStore, files.Select(m => m.Id));
        
        var mod = new Mod
        {
            Name = "Large Entity",
            Files = EntityHashSet<AModFile>.Empty(DataStore),
            Store = DataStore,
        };
        mod = mod with { Files = mod.Files.With(files)};
        mod.EnsureStored();
        var modLoaded = DataStore.Get<Mod>(mod.Id);
        modLoaded!.Files.Should().BeEquivalentTo(set);
    }
}