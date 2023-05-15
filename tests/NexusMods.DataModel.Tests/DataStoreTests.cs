using System.Buffers.Binary;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
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
            Hash = Hash.Zero,
            From = new HashRelativePath((Hash)42L, default),
            Size = Size.From(42L),
            To = new GamePath(GameFolderType.Game, "test.foo")
        }.WithPersist(DataStore);
        foo.DataStoreId.ToString().Should().NotBeEmpty();
        DataStore.Get<FromArchive>(foo.DataStoreId).Should().NotBeNull();
    }

    [Fact]
    public void CompareAndSwapIsAtomic()
    {
        var numThreads = Math.Min(10, Environment.ProcessorCount * 2);
        const ulong numTimes = 100;
        var id = new Id64(EntityCategory.TestData, 0xDEADBEEF);
        var threads = Enumerable.Range(0, numThreads)
            .Select(r => new Thread(() =>
            {
                Span<byte> buffer = stackalloc byte[8];
                for (ulong i = 0; i < numTimes; i++)
                {
                Retry:
                    var oldBuff = DataStore.GetRaw(id);
                    var oldVal = oldBuff == null ? 0 : BinaryPrimitives.ReadUInt64LittleEndian(oldBuff);
                    BinaryPrimitives.WriteUInt64LittleEndian(buffer, oldVal + 1);
                    if (!DataStore.CompareAndSwap(id, buffer, oldBuff))
                    {
                        goto Retry;
                    }
                }
            }))
            .ToArray();
        
        foreach(var thread in threads)
        {
            thread.Start();
        }
        foreach(var thread in threads)
        {
            thread.Join();
        }
        var oldBuff = DataStore.GetRaw(id);
        var oldVal = BinaryPrimitives.ReadUInt64LittleEndian(oldBuff);
        oldVal.Should().Be(numTimes * (ulong)numThreads, "the CAS operation should be atomic");
    }

    [Fact]
    // ReSharper disable once InconsistentNaming
    public void CanStoreLargeEntitiesInDB()
    {
        var files = Enumerable.Range(0, 1024).Select(idx => new FromArchive
        {
            Id = ModFileId.New(),
            From = new HashRelativePath((Hash)(ulong)idx, $"{idx}.file".ToRelativePath()),
            Hash = (Hash)(ulong)idx,
            Size = Size.FromLong(idx),
            To = new GamePath(GameFolderType.Game, $"{idx}.file"),
        }).WithPersist(DataStore)
            .ToList();

        var set = new EntityHashSet<AModFile>(DataStore, files.Select(m => m.DataStoreId));

        var mod = new Mod
        {
            Id = ModId.New(),
            Name = "Large Entity",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore)
        };
        mod = mod with { Files = mod.Files.With(files, x => x.Id) };
        mod.EnsurePersisted(DataStore);
        var modLoaded = DataStore.Get<Mod>(mod.DataStoreId);

        foreach (var itm in set)
        {
            modLoaded!.Files[itm.Id].Should().Be(itm);
        }
    }
}
