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
    private readonly Dictionary<EntityCategory,ColumnFamilyHandle> _columns;
    private readonly ColumnFamilies _families;

    public RocksDbDatastore(AbsolutePath path, DataModelJsonContext jsonOptions)
    {
        _settings = new DbOptions();
        _settings.SetCreateIfMissing();
        _settings.SetCreateMissingColumnFamilies();
        _families = new ColumnFamilies();

        foreach (var column in Enum.GetValues<EntityCategory>())
        {
            _families.Add(Enum.GetName(column), new ColumnFamilyOptions());
        }

        _db = RocksDb.Open(_settings, path.ToString(), _families);
        _mmanager = new RecyclableMemoryStreamManager();
        _jsonOptions = jsonOptions;

        _columns = new Dictionary<EntityCategory, ColumnFamilyHandle>();

        foreach (var column in Enum.GetValues<EntityCategory>())
        {
            var name = Enum.GetName(column);
            if (_db.TryGetColumnFamily(name, out var columnFamilyHandle))
                _columns.Add(column, columnFamilyHandle);
            else
                throw new Exception("hrm");
            //_columns.Add(column, _db.CreateColumnFamily(new ColumnFamilyOptions(), Enum.GetName(column)));
        }


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
        _db.Put(keySpan, data, _columns[value.Category]);
        return new Id(value.Category, hash);
    }

    public T Get<T>(Id id) where T : Entity
    {
        Span<byte> keySpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(keySpan, (ulong)id.Hash);
        return _db.Get(keySpan, str => (T)JsonSerializer.Deserialize(str, _jsonOptions.Entity), _columns[id.Category])!;
    }
    public bool PutRoot(RootType type, Id oldId, Id newId)
    {
        var rootName = new byte[1];
        rootName[0] = (byte)type;
        
        Span<byte> oldIdSpan = stackalloc byte[9];
        oldId.ToTaggedSpan(oldIdSpan);
            
        Span<byte> newIdSpan = stackalloc byte[9];
        newId.ToTaggedSpan(newIdSpan);
        
        lock (_db)
        {
            var existingId = _db.Get(rootName);
            
            if (existingId != null && !existingId.AsSpan().SequenceEqual(oldIdSpan))
                return false;
            
            _db.Put(rootName, newIdSpan);
        }
        return true;
    }

    public Id? GetRoot(RootType type)
    {
        Span<byte> idSpan = stackalloc byte[1];
        idSpan[0] = (byte)type;
        var id = _db.Get(idSpan);
        if (id == null) return null;
        return Id.FromTaggedSpan(id.AsSpan());
    }

    public byte[] GetRaw(ReadOnlySpan<byte> key, EntityCategory category)
    {
        return _db.Get(key, _columns[category]);
    }

    public void PutRaw(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, EntityCategory category)
    {
        _db.Put(key, value, _columns[category]);
    }
}