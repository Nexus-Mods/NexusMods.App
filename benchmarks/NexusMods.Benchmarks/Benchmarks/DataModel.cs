using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;
using Hash = NexusMods.Hashing.xxHash64.Hash;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("DataStore","Test the interfaces on IDataStore")]
public class DataStoreBenchmark : IBenchmark, IDisposable
{
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IDataStore _dataStore;
    private readonly byte[] _rawData;
    private readonly Id64 _rawId;
    private readonly RelativePath _relPutPath;
    private readonly HashRelativePath _fromPutPath;
    private readonly Id _immutableRecord;
    private readonly FromArchive _record;

    public DataStoreBenchmark()
    {
        _temporaryFileManager = new TemporaryFileManager();
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services
                    .AddDataModel();
            }).Build();

        var provider = host.Services.GetRequiredService<IServiceProvider>();
        _dataStore = new SqliteDataStore(_temporaryFileManager.CreateFile(KnownExtensions.Sqlite).Path, provider);
        
        _rawData = new byte[1024];
        Random.Shared.NextBytes(_rawData);
        _rawId = new Id64(EntityCategory.TestData, (ulong)Random.Shared.NextInt64());
        _dataStore.PutRaw(_rawId, _rawData);
        
        _relPutPath = "test.txt".ToRelativePath();
        _fromPutPath = new HashRelativePath(Hash.From((ulong)Random.Shared.NextInt64()), _relPutPath);
        
        _record = new FromArchive
        {
            Id = ModFileId.New(),
            Store = _dataStore,
            From = _fromPutPath,
            Size = Size.From(1024),
            Hash = Hash.From(42),
            To = new GamePath(GameFolderType.Game, "test.txt")
        };
        _immutableRecord = _dataStore.Put(_record);
    }

    [Benchmark]
    public void PutRawData()
    {
        _dataStore.PutRaw(_rawId, _rawData);
    }

    [Benchmark]
    public void GetRawData()
    {
        _dataStore.GetRaw(_rawId);
    }
    
    [Benchmark]
    public void PutImmutable()
    {
        var record = new FromArchive
        {
            Id = ModFileId.New(),
            Store = _dataStore,
            From = _fromPutPath,
            Size = Size.From(1024),
            Hash = Hash.From(42),
            To = new GamePath(GameFolderType.Game, "test.txt")
        };
        var id = _dataStore.Put(record);
    }

    [Benchmark]
    public void PutImmutableById()
    {
        _dataStore.Put(_immutableRecord, _record);
    }
    
    [Benchmark]
    public void GetImmutable()
    {
        _dataStore.Get<FromArchive>(_immutableRecord);
    }
    
    [Benchmark]
    public void GetImmutableCached()
    {
        _dataStore.Get<FromArchive>(_immutableRecord, true);
    }
    
    public void Dispose()
    {
        _temporaryFileManager.Dispose();
    }
}