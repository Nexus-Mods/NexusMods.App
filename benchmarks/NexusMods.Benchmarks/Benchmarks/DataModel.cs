using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Installers.DTO.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.App.BuildInfo;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.DataModel;
using NexusMods.Paths;
using Hash = NexusMods.Hashing.xxHash64.Hash;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("DataStore", "Test the interfaces on IDataStore")]
public class DataStoreBenchmark : IBenchmark, IDisposable
{
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IDataStore _dataStore;
    private readonly byte[] _rawData;
    private readonly Id64 _rawId;
    private readonly IId _immutableRecord;
    private readonly StoredFile _record;

    public DataStoreBenchmark()
    {
        _temporaryFileManager = new TemporaryFileManager(FileSystem.Shared);
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services
                    .AddDataModel()
                    .Validate();
            }).Build();

        var provider = host.Services.GetRequiredService<IServiceProvider>();
        _dataStore = new SqliteDataStore(
            provider.GetRequiredService<ILogger<SqliteDataStore>>(),
            new DataModelSettings(FileSystem.Shared), provider);

        _rawData = new byte[1024];
        Random.Shared.NextBytes(_rawData);
        _rawId = new Id64(EntityCategory.TestData, (ulong)Random.Shared.NextInt64());
        _dataStore.PutRaw(_rawId, _rawData);
        _record = new StoredFile
        {
            Id = ModFileId.NewId(),
            Size = Size.FromLong(1024),
            Hash = Hash.From(42),
            To = new GamePath(LocationId.Game, "test.txt")
        };
        _immutableRecord = _dataStore.Put(_record);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _temporaryFileManager.Dispose();
    }

    [Benchmark]
    public void PutRawData()
    {
        _dataStore.PutRaw(_rawId, _rawData);
    }

    [Benchmark]
    public byte[]? GetRawData()
    {
        return _dataStore.GetRaw(_rawId);
    }

    [Benchmark]
    public IId PutImmutable()
    {
        var record = new StoredFile
        {
            Id = ModFileId.NewId(),
            Size = Size.FromLong(1024),
            Hash = Hash.From(42),
            To = new GamePath(LocationId.Game, "test.txt")
        };
        return _dataStore.Put(record);
    }

    [Benchmark]
    public void PutImmutableById()
    {
        _dataStore.Put(_immutableRecord, _record);
    }

    [Benchmark]
    public StoredFile? GetImmutable()
    {
        return _dataStore.Get<StoredFile>(_immutableRecord);
    }

    [Benchmark]
    public StoredFile? GetImmutableCached()
    {
        return _dataStore.Get<StoredFile>(_immutableRecord, true);
    }
}
