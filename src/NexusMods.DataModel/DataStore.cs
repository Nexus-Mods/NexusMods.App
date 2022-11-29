using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Microsoft.IO;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using RocksDbSharp;

namespace NexusMods.DataModel;

public class RocksDbDatastore : IDataStore
{
    private readonly DbOptions _settings;
    private readonly RocksDb _db;
    private readonly RecyclableMemoryStreamManager _mmanager;
    private readonly DataModelJsonContext _jsonOptions;

    public RocksDbDatastore(AbsolutePath path, DataModelJsonContext jsonOptions)
    {
        _settings = new DbOptions();
        _settings.SetCreateIfMissing();
        _db = RocksDb.Open(_settings, path.ToString());
        _mmanager = new RecyclableMemoryStreamManager();
        _jsonOptions = jsonOptions;
    }
    public Id Put<T>(T value) where T : Entity
    {
        using var stream = new RecyclableMemoryStream(_mmanager);
        JsonSerializer.Serialize(stream, value, _jsonOptions.Entity);
        stream.Position = 0;
        var data = (ReadOnlySpan<byte>)stream.GetSpan()[..(int)stream.Length];
        var hash = data.XxHash64();
        Span<byte> keySpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(keySpan, (ulong)hash);
        _db.Put(keySpan, data);
        return new Id(hash);
    }

    public T Get<T>(Id id) where T : Entity
    {
        Span<byte> keySpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(keySpan, (ulong)id.Hash);
        return _db.Get(keySpan, str => (T)JsonSerializer.Deserialize(str, _jsonOptions.Entity))!;
    }
}