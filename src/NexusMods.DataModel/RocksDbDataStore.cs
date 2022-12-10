using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly Dictionary<EntityCategory,ColumnFamilyHandle> _columns;
    private readonly ColumnFamilies _families;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    public RocksDbDatastore(AbsolutePath path, IServiceProvider provider)
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
        
        // Make this lazy to avoid the cycle reference of store -> options -> converter -> store
        _jsonOptions = new Lazy<JsonSerializerOptions>(provider.GetRequiredService<JsonSerializerOptions>);

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
        JsonSerializer.Serialize(stream, value, _jsonOptions.Value);
        stream.Position = 0;
        var span = (ReadOnlySpan<byte>)stream.GetSpan();

        Hash hash;
        
        // Span returned may be smaller then the entire size if the buffer had to be resized
        if (span.Length >= stream.Length)
        {
            hash = span.XxHash64();
            Span<byte> keySpan = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(keySpan, hash);
            _db.Put(keySpan, span[..(int)stream.Length], _columns[value.Category]);
        }
        else
        {
            var data = stream.GetBuffer();
            hash = ((ReadOnlySpan<byte>)data.AsSpan())[..(int)stream.Length].XxHash64();
            Span<byte> keySpan = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(keySpan, hash);
            _db.Put(keySpan, data.AsSpan()[..(int)stream.Length], _columns[value.Category]);
        }
        
        return new Id64(value.Category, hash);
    }

    public void Put<T>(Id id, T value) where T : Entity
    {
        using var stream = new RecyclableMemoryStream(_mmanager);
        JsonSerializer.Serialize(stream, value, _jsonOptions.Value);
        stream.Position = 0;
        var span = (ReadOnlySpan<byte>)stream.GetSpan();
        Span<byte> keySpan = stackalloc byte[id.SpanSize];
        id.ToSpan(keySpan);
        
        // Span returned may be smaller then the entire size if the buffer had to be resized
        if (span.Length >= stream.Length)
        {
            _db.Put(keySpan, span[..(int)stream.Length], _columns[value.Category]);
        }
        else
        {
            _db.Put(keySpan, stream.GetBuffer().ToArray(), _columns[value.Category]);
        }

    }

    public T? Get<T>(Id id) where T : Entity
    {
        Span<byte> keySpan = stackalloc byte[id.SpanSize];
        id.ToSpan(keySpan);
        return _db.Get(keySpan, str => 
                JsonSerializer.Deserialize<T>(str, _jsonOptions.Value), 
            _columns[id.Category]);
    }
    
    public bool PutRoot(RootType type, Id oldId, Id newId)
    {
        var rootName = new byte[1];
        rootName[0] = (byte)type;
            
        Span<byte> newIdSpan = stackalloc byte[newId.SpanSize + 1];
        newId.ToTaggedSpan(newIdSpan);
        
        lock (_db)
        {
            var existingBytes = _db.Get(rootName);
            if (existingBytes != null)
            {
                var existingId = Id.FromTaggedSpan(existingBytes);

                if (!existingId.Equals(oldId))
                    return false;
            }
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

    public byte[]? GetRaw(ReadOnlySpan<byte> key, EntityCategory category)
    {
        return _db.Get(key, _columns[category]);
    }

    public void PutRaw(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, EntityCategory category)
    {
        _db.Put(key, value, _columns[category]);
    }
}