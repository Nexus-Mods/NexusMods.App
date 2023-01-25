using System.Collections.Immutable;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class LMDBDataStoreTests : ADataModelTest<LMDBDataStoreTests>
{
    public LMDBDataStoreTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public void CanGetAndSetHashedValues()
    {
        var foo = new FromArchive
        {
            Id = ModFileId.New(),
            Store = DataStore,
            Hash = Hash.Zero,
            From = new HashRelativePath((Hash)42L, Array.Empty<RelativePath>()),
            Size = (Size)42L,
            To = new GamePath(GameFolderType.Game, "test.foo")
        };
        foo.DataStoreId.ToString().Should().NotBeEmpty();
        DataStore.Get<FromArchive>(foo.DataStoreId).Should().NotBeNull();
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
            Id = ModFileId.New(),
            From = new HashRelativePath((Hash)(ulong)idx, $"{idx}.file".ToRelativePath()),
            Hash = (Hash)(ulong)idx,
            Size = Size.From(idx),
            Store = DataStore,
            To = new GamePath(GameFolderType.Game, $"{idx}.file"),
        }).ToList();
        
        var set = new EntityHashSet<AModFile>(DataStore, files.Select(m => m.DataStoreId));
        
        var mod = new Mod
        {
            Id = ModId.New(),
            Name = "Large Entity",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
            Store = DataStore,
        };
        mod = mod with { Files = mod.Files.With(files, x => x.Id)};
        mod.EnsureStored();
        var modLoaded = DataStore.Get<Mod>(mod.DataStoreId);

        foreach (var itm in set)
        {
            modLoaded!.Files[itm.Id].Should().Be(itm);
        }
    }
}