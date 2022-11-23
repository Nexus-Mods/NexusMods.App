using System.Text.Json;
using System.Text.Json.Serialization;
using FASTER.core;
using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class DataStore
{
    private readonly FasterKVSettings<Id,IVersionedObject> _kvSettings;
    private readonly FasterKV<Id,IVersionedObject> _store;
    private readonly DataModelJsonContext _jsonCtx;

    public DataStore(AbsolutePath path, DataModelJsonContext ctx)
    {
        _jsonCtx = ctx;
        _kvSettings = new FasterKVSettings<Id, IVersionedObject>(path.ToString())
        {
            KeySerializer = () => new KeySerializer(),
            ValueSerializer = () => new ValueSerializer(ctx),
            EqualityComparer = new KeyEqualityComparer(),
        };
        _store = new FasterKV<Id, IVersionedObject>(_kvSettings);
    }
    public Id StoreRoot<T>(T o) where T : IVersionedObject
    {
        using var session = new Session(this);
        session.Store(o);
        return o.Id;
    }

    public async Task FlushChanges()
    {
        await _store.TakeFullCheckpointAsync(CheckpointType.FoldOver);
    }
    
    private class KeyEqualityComparer : IFasterEqualityComparer<Id>
    {
        public long GetHashCode64(ref Id k)
        {
            return k.GetHashCode64();
        }

        public bool Equals(ref Id k1, ref Id k2)
        {
            return k1.Equals(k2);
        }
    }

    private class KeySerializer : BinaryObjectSerializer<Id>
    {
        public override void Deserialize(out Id obj)
        {
            Span<byte> buffer = stackalloc byte[16];
            var read = reader.Read(buffer);
            if (read != 16)
                throw new NotImplementedException();
            obj = Id.From(buffer);
        }

        public override void Serialize(ref Id obj)
        {
            Span<byte> buffer = stackalloc byte[16];
            obj.Write(buffer);
            writer.Write(buffer);
        }
    }
    private class ValueSerializer : IObjectSerializer<IVersionedObject>
    {
        private Stream _stream = Stream.Null;
        private readonly DataModelJsonContext _ctx;
        public ValueSerializer(DataModelJsonContext ctx)
        {
            _ctx = ctx;
        }

        public void BeginSerialize(Stream stream)
        {
            _stream = stream;
        }

        public void Serialize(ref IVersionedObject obj)
        {
            JsonSerializer.Serialize(_stream, obj, _ctx.IVersionedObject);
        }

        public void EndSerialize()
        {
            _stream = Stream.Null;
        }

        public void BeginDeserialize(Stream stream)
        {
            _stream = stream;
        }

        public void Deserialize(out IVersionedObject obj)
        {
            obj = JsonSerializer.Deserialize<IVersionedObject>(_stream)!;
        }

        public void EndDeserialize()
        {
            _stream = Stream.Null;
        }
    }
    
    private class Session : IDisposable, IDataStore
    {
        private readonly DataStore _dataStore;
        private readonly ClientSession<Id, IVersionedObject, IVersionedObject, IVersionedObject, Empty, IFunctions<Id, IVersionedObject, IVersionedObject, IVersionedObject, Empty>> _session;

        public Session(DataStore store)
        {
            _dataStore = store;
            _session = _dataStore._store.NewSession(new SimpleFunctions<Id, IVersionedObject>((a, _) => a));
        }
        public void Dispose()
        {
            _session.CompletePending();
            _session.Dispose();
        }

        public IVersionedObject Load(Id id)
        {
            var (status, output) = _session.Read(id);
            if (!status.Found)
                throw new Exception("Not Found");
            return output!;
        }

        public Id Store(IVersionedObject o)
        {
            if (!o.IsDirty) return o.Id;

            if (o is AParentObject po)
                po.PersistChildren(this);
            var id = Id.New();
            o.Id.Set(id);
            var result = _session.Upsert(ref id, ref o);
            if (!result.IsCompletedSuccessfully)
                throw new Exception($"Serialization Exception {result}");
            return id;
        }
    }

    public IVersionedObject Load(Id id)
    {
        var session = new Session(this);
        return session.Load(id);
    }

    public T Load<T>(Id id) where T : IVersionedObject
    {
        return (T)Load(id);
    }
}