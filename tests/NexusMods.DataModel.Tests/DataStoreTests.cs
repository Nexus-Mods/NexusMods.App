using System.Buffers.Binary;
using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using ModFileId = NexusMods.Abstractions.Loadouts.Mods.ModFileId;

namespace NexusMods.DataModel.Tests;

public class DataStoreTests
{
    public IDataStore DataStore { get; set; }

    public DataStoreTests(IDataStore store) => DataStore = store;

    [Fact]
    public void CanGetAndSetHashedValues()
    {
        var foo = new StoredFile
        {
            Id = ModFileId.NewId(),
            Hash = Hash.Zero,
            Size = Size.From(42L),
            To = new GamePath(LocationId.Game, "test.foo")
        }.WithPersist(DataStore);
        foo.DataStoreId.ToString().Should().NotBeEmpty();
        DataStore.Get<StoredFile>(foo.DataStoreId).Should().NotBeNull();
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
        var files = Enumerable.Range(0, 1024).Select(idx => new StoredFile
        {
            Id = ModFileId.NewId(),
            Hash = (Hash)(ulong)idx,
            Size = Size.FromLong(idx),
            To = new GamePath(LocationId.Game, $"{idx}.file"),
        }).WithPersist(DataStore)
            .ToList();

        var set = new EntityHashSet<AModFile>(DataStore, files.Select(m => m.DataStoreId));

        var mod = new Mod
        {
            Id = ModId.NewId(),
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
