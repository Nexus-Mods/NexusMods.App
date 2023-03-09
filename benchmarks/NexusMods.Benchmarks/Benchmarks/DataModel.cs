using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;
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
    private readonly HashRelativePath _fromPutPath;
    private readonly IId _immutableRecord;
    private readonly FromArchive _record;

    public DataStoreBenchmark()
    {
        _temporaryFileManager = new TemporaryFileManager();
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
            _temporaryFileManager.CreateFile(KnownExtensions.Sqlite).Path, provider,
            provider.GetRequiredService<IMessageProducer<RootChange>>(),
            provider.GetRequiredService<IMessageConsumer<RootChange>>(),
        provider.GetRequiredService<IMessageProducer<IdPut>>(),
        provider.GetRequiredService<IMessageConsumer<IdPut>>());

        _rawData = new byte[1024];
        Random.Shared.NextBytes(_rawData);
        _rawId = new Id64(EntityCategory.TestData, (ulong)Random.Shared.NextInt64());
        _dataStore.PutRaw(_rawId, _rawData);

        var relPutPath = "test.txt".ToRelativePath();
        _fromPutPath = new HashRelativePath(Hash.From((ulong)Random.Shared.NextInt64()), relPutPath);

        _record = new FromArchive
        {
            Id = ModFileId.New(),
            From = _fromPutPath,
            Size = Size.From(1024),
            Hash = Hash.From(42),
            To = new GamePath(GameFolderType.Game, "test.txt")
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
        var record = new FromArchive
        {
            Id = ModFileId.New(),
            From = _fromPutPath,
            Size = Size.From(1024),
            Hash = Hash.From(42),
            To = new GamePath(GameFolderType.Game, "test.txt")
        };
        return _dataStore.Put(record);
    }

    [Benchmark]
    public void PutImmutableById()
    {
        _dataStore.Put(_immutableRecord, _record);
    }

    [Benchmark]
    public FromArchive? GetImmutable()
    {
        return _dataStore.Get<FromArchive>(_immutableRecord);
    }

    [Benchmark]
    public FromArchive? GetImmutableCached()
    {
        return _dataStore.Get<FromArchive>(_immutableRecord, true);
    }
}
