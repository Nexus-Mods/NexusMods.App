using System.Reactive.Subjects;
using System.Text.Json;
using LightningDB;
using Microsoft.IO;
using NexusMods.DataModel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using RocksDbSharp;

namespace NexusMods.DataModel;

public class LMDBDataStore : IDataStore
{
    private readonly LightningEnvironment _db;
    private readonly RecyclableMemoryStreamManager _mmanager;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    private Subject<(Id Id, Entity Entity)> _changes = new();
    public IObservable<(Id Id, Entity Entity)> Changes => _changes;

    public LMDBDataStore(AbsolutePath path, IServiceProvider provider)
    {
        var settings = new DbOptions();
        settings.SetCreateIfMissing();

        _db = new LightningEnvironment(path.ToString());
        _db.Open();
        
        

        _mmanager = new RecyclableMemoryStreamManager();
        
        // Make this lazy to avoid the cycle reference of store -> options -> converter -> store
        _jsonOptions = new Lazy<JsonSerializerOptions>(provider.GetRequiredService<JsonSerializerOptions>);
        
    }
    public Id Put<T>(T value) where T : Entity
    {
        using var stream = new RecyclableMemoryStream(_mmanager);
        JsonSerializer.Serialize(stream, value, _jsonOptions.Value);
        stream.Position = 0;
        var span = (ReadOnlySpan<byte>)stream.GetSpan();

        var id = IdEmpty.Empty;
        
        // Span returned may be smaller then the entire size if the buffer had to be resized
        if (span.Length >= stream.Length)
        {
            var hash = span.XxHash64();
            id = new Id64(value.Category, hash);
            Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
            id.ToTaggedSpan(keySpan);

            using var tx = _db.BeginTransaction();
            using var db = tx.OpenDatabase();
            tx.Put(db, keySpan, span[..(int)stream.Length]);
            tx.Commit();
        }
        else
        {
            var data = stream.GetBuffer();
            var hash = ((ReadOnlySpan<byte>)data.AsSpan())[..(int)stream.Length].XxHash64();
            id = new Id64(value.Category, hash);
            Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
            id.ToTaggedSpan(keySpan);
            
            using var tx = _db.BeginTransaction();
            using var db = tx.OpenDatabase();
            tx.Put(db, keySpan, data.AsSpan()[..(int)stream.Length]);
            tx.Commit();
        }

        _changes.OnNext((id, value));
        return id;
    }

    public void Put<T>(Id id, T value) where T : Entity
    {
        using var stream = new RecyclableMemoryStream(_mmanager);
        JsonSerializer.Serialize(stream, value, _jsonOptions.Value);
        stream.Position = 0;
        var span = (ReadOnlySpan<byte>)stream.GetSpan();
        Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
        id.ToTaggedSpan(keySpan);
        using var tx = _db.BeginTransaction();
        using var db = tx.OpenDatabase();
        
        // Span returned may be smaller then the entire size if the buffer had to be resized
        if (span.Length >= stream.Length)
        {
            tx.Put(db, keySpan, span[..(int)stream.Length]);
        }
        else
        {
            tx.Put(db, keySpan, stream.GetBuffer().ToArray());
        }
        tx.Commit();
        _changes.OnNext((id, value));
    }

    public T? Get<T>(Id id) where T : Entity
    {
        Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
        id.ToTaggedSpan(keySpan);
        
        using var tx = _db.BeginTransaction();
        using var db = tx.OpenDatabase();
        var (resultCode, key, value) = tx.Get(db, keySpan);
        if (resultCode != MDBResultCode.Success)
            return null;
        return JsonSerializer.Deserialize<T>(value.AsSpan(), _jsonOptions.Value);
        
    }
    
    public bool PutRoot(RootType type, Id oldId, Id newId)
    {
        
        Span<byte> newIdSpan = stackalloc byte[newId.SpanSize + 1];
        newId.ToTaggedSpan(newIdSpan);
        
        lock (_db)
        {
            var existingId = GetRoot(type);
            if (existingId!= null)
            {
                if (!existingId.Equals(oldId))
                    return false;
            }
            Id rootId = new RootId(type);
            Span<byte> rootIdSpan = stackalloc byte[rootId.SpanSize + 1];
            rootId.ToTaggedSpan(rootIdSpan);
            using var tx = _db.BeginTransaction();
            using var db = tx.OpenDatabase();
            tx.Put(db, rootIdSpan, newIdSpan);
            tx.Commit();
        }
        return true;
    }

    public Id? GetRoot(RootType type)
    {
        Id rootId = new RootId(type);
        Span<byte> idSpan = stackalloc byte[rootId.SpanSize + 1];
        rootId.ToTaggedSpan(idSpan);
        using var tx = _db.BeginTransaction();
        using var db = tx.OpenDatabase();
        var (resultCode, key, value) = tx.Get(db, idSpan);
        if (resultCode != MDBResultCode.Success)
            return null;
        return Id.FromTaggedSpan(value.AsSpan());
    }

    public byte[]? GetRaw(ReadOnlySpan<byte> key, EntityCategory category)
    {
        using var tx = _db.BeginTransaction();
        using var db = tx.OpenDatabase();
        var (resultCode, _, value) = tx.Get(db, key);
        if (resultCode != MDBResultCode.Success)
            return null;
        return value.CopyToNewArray();
    }

    public void PutRaw(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, EntityCategory category)
    {
        using var tx = _db.BeginTransaction();
        using var db = tx.OpenDatabase();
        tx.Put(db, key, value);
        tx.Commit();
    }

    public IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity
    {
        using var tx = _db.BeginTransaction(TransactionBeginFlags.ReadOnly);
        using var db = tx.OpenDatabase();
        var cursor = tx.CreateCursor(db);
        
        Span<byte> key = stackalloc byte[prefix.SpanSize + 1];
        prefix.ToTaggedSpan(key);

        var result = cursor.SetRange(key);
        
        while (result == MDBResultCode.Success)
        {
            var (_, keySpan, valueSpan) = cursor.GetCurrent();
            
            var seekKey = Id.FromTaggedSpan(keySpan.AsSpan());
            if (seekKey.IsPrefixedBy(prefix))
            {
                var value = JsonSerializer.Deserialize<Entity>(valueSpan.AsSpan(), _jsonOptions.Value);
                if (value is T tc)
                {
                    yield return tc;
                }
            }
            else
            {
                break;
            }

            result = cursor.Next();
        }
    }
}