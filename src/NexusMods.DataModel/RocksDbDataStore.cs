using System.Buffers.Binary;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using RocksDbSharp;

namespace NexusMods.DataModel;

public class RocksDbDatastore : IDataStore
{
    private readonly Task<RocksDb> _db;
    private readonly RecyclableMemoryStreamManager _mmanager;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly TaskCompletionSource<RocksDb> _tsc;

    private Subject<(Id Id, Entity Entity)> _changes = new();
    public IObservable<(Id Id, Entity Entity)> Changes => _changes;

    public RocksDbDatastore(AbsolutePath path, IServiceProvider provider)
    {
        var settings = new DbOptions();
        settings.SetCreateIfMissing();

        _tsc = new TaskCompletionSource<RocksDb>();
        new Thread(() =>
        {
            try
            {
                _tsc.TrySetResult(RocksDb.Open(settings, path.ToString()));
            }
            catch (Exception ex)
            {
                _tsc.SetException(ex);
            }
        }).Start();
        _db = _tsc.Task;
        
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
            _db.Result.Put(keySpan, span[..(int)stream.Length]);
        }
        else
        {
            var data = stream.GetBuffer();
            var hash = ((ReadOnlySpan<byte>)data.AsSpan())[..(int)stream.Length].XxHash64();
            id = new Id64(value.Category, hash);
            Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
            id.ToTaggedSpan(keySpan);
            _db.Result.Put(keySpan, data.AsSpan()[..(int)stream.Length]);
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
        
        // Span returned may be smaller then the entire size if the buffer had to be resized
        if (span.Length >= stream.Length)
        {
            _db.Result.Put(keySpan, span[..(int)stream.Length]);
        }
        else
        {
            _db.Result.Put(keySpan, stream.GetBuffer().ToArray());
        }
        _changes.OnNext((id, value));
    }

    public T? Get<T>(Id id) where T : Entity
    {
        Span<byte> keySpan = stackalloc byte[id.SpanSize + 1];
        id.ToTaggedSpan(keySpan);
        return _db.Result.Get(keySpan, str => 
                JsonSerializer.Deserialize<T>(str, _jsonOptions.Value));
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
            _db.Result.Put(rootIdSpan, newIdSpan);
        }
        return true;
    }

    public Id? GetRoot(RootType type)
    {
        Id rootId = new RootId(type);
        Span<byte> idSpan = stackalloc byte[rootId.SpanSize + 1];
        rootId.ToTaggedSpan(idSpan);
        var id = _db.Result.Get(idSpan);
        if (id == null) return null;
        return Id.FromTaggedSpan(id.AsSpan());
    }

    public byte[]? GetRaw(ReadOnlySpan<byte> key, EntityCategory category)
    {
        return _db.Result.Get(key);
    }

    public void PutRaw(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, EntityCategory category)
    {
        _db.Result.Put(key, value);
    }

    public IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity
    {
        Span<byte> key = stackalloc byte[prefix.SpanSize + 1];
        prefix.ToTaggedSpan(key);

        var ro = new ReadOptions();
        ro.SetTotalOrderSeek(true);

        using var iterator = _db.Result.NewIterator(readOptions:ro);
        iterator.Seek(key);


        while (iterator.Valid())
        {
            var seekKey = Id.FromTaggedSpan(iterator.GetKeySpan());
            if (seekKey.IsPrefixedBy(prefix))
            {
                var valSpan = iterator.GetValueSpan();
                var value = JsonSerializer.Deserialize<Entity>(valSpan, _jsonOptions.Value);
                if (value is T tc)
                {
                    yield return tc;
                }
            }
            else
            {
                break;
            }

            iterator.Next();
        }
        
    }
}