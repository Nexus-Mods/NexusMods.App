using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Loadouts.ModFiles;

namespace NexusMods.DataModel.Abstractions;

public struct EntityHashSet<T> : IEmptyWithDataStore<EntityHashSet<T>>, IEnumerable<T>, IEquatable<EntityHashSet<T>>
where T : Entity
{
    public static EntityHashSet<T> Empty(IDataStore store) => new(store);
    private readonly ImmutableHashSet<Id> _coll;
    private readonly IDataStore _store;

    public EntityHashSet(IDataStore store)
    {
        _coll = ImmutableHashSet<Id>.Empty;
        _store = store;
    }

    private EntityHashSet(IDataStore store, ImmutableHashSet<Id> coll)
    {
        _coll = coll;
        _store = store;
    }

    public EntityHashSet(IDataStore store, IEnumerable<Id> ids)
    {
        _coll = ImmutableHashSet.CreateRange(ids);
        _store = store;
    }

    public EntityHashSet<T> With(T val)
    {
        return new EntityHashSet<T>(_store, _coll.Add(val.DataStoreId));
    }

    public EntityHashSet<T> Without(T val)
    {
        return new EntityHashSet<T>(_store, _coll.Remove(val.DataStoreId));
    }
    
    public EntityHashSet<T> Without(IEnumerable<T> vals)
    {
        var newColl = vals.Aggregate(_coll, (acc, itm) => acc.Remove(itm.DataStoreId));
        return new EntityHashSet<T>(_store, newColl);
    }

    /// <summary>
    /// Transforms this hashset returning a new hashset. If `func` returns null for a given
    /// value, the item is removed from the set
    /// </summary>
    /// <param name="func"></param>
    /// <returns></returns>
    public EntityHashSet<T> Keep(Func<T, T?> func)
    {
        var coll = _coll;
        foreach (var id in _coll)
        {
            var result = func((T)_store.Get<Entity>(id)!);
            if (result == null)
            {
                coll = coll.Remove(id);
            }
            else if (!result.DataStoreId.Equals(id))
                coll = coll.Remove(id).Add(result.DataStoreId);
        }
        return ReferenceEquals(coll, _coll) ? this : new EntityHashSet<T>(_store, coll);
    }

    public bool Contains(T val)
    {
        return _coll.Contains(val.DataStoreId);
    }

    public IEnumerable<Id> Ids => _coll;
    public int Count => _coll.Count;
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var itm in _coll)
            yield return (T)_store.Get<Entity>(itm)!;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public EntityHashSet<T> With(IEnumerable<T> items)
    {
        var builder = _coll.ToBuilder();
        foreach (var item in items)
        {
            builder.Add(item.DataStoreId);
        }

        return new EntityHashSet<T>(_store, builder.ToImmutable());
    }

    public bool Equals(EntityHashSet<T> other)
    {
        if (other.Count != Count) return false;
        foreach (var itm in other._coll)
            if (!_coll.Contains(itm))
                return false;
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityHashSet<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_coll, _store);
    }
}

public class EntityHashSetConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _services;

    public EntityHashSetConverterFactory(IServiceProvider services)
    {
        _services = services;
    }
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityHashSet<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_services.GetRequiredService(typeof(EntityHashSetConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()));
    }
}

public class EntityHashSetConverter<T> : JsonConverter<EntityHashSet<T>>
    where T : Entity
{
    private Lazy<IDataStore> _store;
    
    public EntityHashSetConverter(IServiceProvider provider)
    {
        _store = new Lazy<IDataStore>(provider.GetRequiredService<IDataStore>);
    }
    
    public override EntityHashSet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");
        reader.Read();

        var lst = new List<Id>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            lst.Add(JsonSerializer.Deserialize<Id>(ref reader, options)!);
            reader.Read();
        }
        
        return new EntityHashSet<T>(_store.Value, lst);
    }

    public override void Write(Utf8JsonWriter writer, EntityHashSet<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var itm in value.Ids)
            JsonSerializer.Serialize(writer, itm, options);
        writer.WriteEndArray();
    }
}