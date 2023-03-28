using System.Buffers.Binary;
using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
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
            Hash = Hash.Zero,
            From = new HashRelativePath((Hash)42L, Array.Empty<RelativePath>()),
            Size = (Size)42L,
            To = new GamePath(GameFolderType.Game, "test.foo")
        }.WithPersist(DataStore);
        foo.DataStoreId.ToString().Should().NotBeEmpty();
        DataStore.Get<FromArchive>(foo.DataStoreId).Should().NotBeNull();
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
