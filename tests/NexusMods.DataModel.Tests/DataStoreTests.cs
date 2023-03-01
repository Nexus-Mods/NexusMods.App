using System.Buffers.Binary;
using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Tests;

public class DataStoreTests
{
    private readonly ILogger<DataStoreTests> _logger;

    public DataStoreTests(ILogger<DataStoreTests> logger, IDataStore store)
    {
        _logger = logger;
        DataStore = store;
    }

    public IDataStore DataStore { get; set; }

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

    [Fact]
    public async Task CanGetRootUpdates()
    {
        var src = new List<Id>();
        var destQ = new ConcurrentQueue<Id>();

        using var _ = DataStore.Changes.Subscribe(c => destQ.Enqueue(c.To));

        var oldId = DataStore.GetRoot(RootType.Tests) ?? IdEmpty.Empty;

        var bytes = new byte[4];
        
        foreach (var itm in Enumerable.Range(0, 128))
        {
            var newId = new Id64(EntityCategory.TestData, (ulong)itm);
            BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)itm);
            DataStore.PutRaw(newId, bytes);
            DataStore.PutRoot(RootType.Tests, oldId, newId).Should().BeTrue();
            src.Add(newId);
            oldId = newId;
        }
        
        var attempts = 0;
        while (destQ.IsEmpty && attempts < 1000)
        {
            _logger.LogInformation("Waiting for changes...");
            await Task.Delay(200);
            attempts++;
        }

        // Cant' be exact about this test because other things being tested may be generating changes
        var dest = destQ.ToList();
        dest.Should().NotBeEmpty();
    }
}
