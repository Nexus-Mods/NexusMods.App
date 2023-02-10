using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS8604

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(EntityDictionaryConverterFactory))]
public struct EntityDictionary<TK, TV> : 
    IEmptyWithDataStore<EntityDictionary<TK, TV>>, 
    IEnumerable<KeyValuePair<TK, TV>>,
    IWalkable<Entity>
    where TV : Entity where TK : notnull
{
    private readonly ImmutableDictionary<TK, Id> _coll;
    private readonly IDataStore _store;
    
    public EntityDictionary(IDataStore store)
    {
        _store = store;
        _coll = ImmutableDictionary<TK, Id>.Empty;
    }
    public EntityDictionary(IDataStore store, IEnumerable<KeyValuePair<TK, Id>> kvs)
    {
        _store = store;
        _coll = ImmutableDictionary.CreateRange(kvs);
    }

    public EntityDictionary<TK, TV> With(TK key, TV val)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.SetItem(key, val.DataStoreId));
    }
    
    
    /// <summary>
    /// Adds val to the collection using keyFn(val) as the key
    /// </summary>
    /// <param name="val"></param>
    /// <param name="keyFn"></param>
    /// <returns></returns>
    public EntityDictionary<TK, TV> With(TV val, Func<TV, TK> keyFn)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.SetItem(keyFn(val), val.DataStoreId));
    }
    
    public EntityDictionary<TK, TV> With(IEnumerable<TV> val, Func<TV, TK> keyFn)
    {
        var builder = _coll.ToBuilder();
        foreach (var v in val)
        {
            builder[keyFn(v)] = v.DataStoreId;
        }
        return new EntityDictionary<TK, TV>(_store, builder.ToImmutable());
    }

    public EntityDictionary<TK, TV> Without(TK key)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.Remove(key));
    }

    public bool ContainsKey(TK val)
    {
        return _coll.ContainsKey(val);
    }

    public TV this[TK k] => _store.Get<TV>(_coll[k])!;

    public IEnumerable<KeyValuePair<TK, Id>> Ids => _coll;
    public int Count => _coll.Count;
    public IEnumerable<TV> Values
    {
        get
        {
            foreach (var id in _coll.Values)
                yield return _store.Get<TV>(id)!;
        }
    }

    public IEnumerable<TK> Keys => _coll.Keys;

    public static EntityDictionary<TK, TV> Empty(IDataStore store) => new(store);
    public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
    {
        foreach (var (key, value) in _coll)
            yield return KeyValuePair.Create(key, _store.Get<TV>(value)!);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <summary>
    /// Transforms a value found at at given id with a given function.
    /// If the function returns null, the value is removed from the dictionary.
    /// </summary>
    /// <param name="???"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public EntityDictionary<TK, TV> Keep(TK key, Func<TV?, TV?> func)
    {
        var id = _coll[key];
        var val = _store.Get<TV>(id);
        var newVal = func(val);
        if (ReferenceEquals(newVal, val))
            return this;
        return newVal is null ? Without(key) : With(key, newVal);
    }
    
    /// <summary>
    /// Transforms all values in the dictionary with a given function.
    /// </summary>
    /// <param name="???"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public EntityDictionary<TK, TV> Keep(Func<TV?, TV?> func)
    {
        var modified = false;
        var builder = ImmutableDictionary.CreateBuilder<TK, Id>();
        foreach (var (key, id) in _coll)
        {
            var val = _store.Get<TV>(id);
            var newVal = func(val);
            if (ReferenceEquals(newVal, val))
            {
                builder.Add(key, id);
                continue;
            }

            if (newVal is not null)
                builder.Add(key, newVal.DataStoreId);

            modified = true;
        }
        return modified ? new EntityDictionary<TK, TV>(_store, builder) : this;
    }

    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return Values.Aggregate(initial, (state, itm) =>
        {
            state = visitor(state, itm);
            return itm.Walk(visitor, state);
        });
    }
}

public class EntityDictionaryConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _provider;

    public EntityDictionaryConverterFactory(IServiceProvider provider)
    {
        _provider = provider;
    }
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 2 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityDictionary<,>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_provider.GetRequiredService(typeof(EntityDictionaryConverter<,>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

public class EntityDictionaryConverter<TK, TV> : JsonConverter<EntityDictionary<TK, TV>>
    where TV : Entity where TK : notnull
{
    private readonly IDataStore _store;

    public EntityDictionaryConverter(IDataStore store)
    {
        _store = store;
    }

    public override EntityDictionary<TK, TV> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");
        reader.Read();

        var lst = new List<KeyValuePair<TK, Id>>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var tk = JsonSerializer.Deserialize<TK>(ref reader, options)!;
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                throw new JsonException("Found end of array when expecting dictionary value");
            var tv = JsonSerializer.Deserialize<Id>(ref reader, options);
            reader.Read();
            lst.Add(new KeyValuePair<TK, Id>(tk, tv));
        }
        
        return new EntityDictionary<TK, TV>(_store, lst);
    }

    public override void Write(Utf8JsonWriter writer, EntityDictionary<TK, TV> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var (k, id) in value.Ids)
        {
            JsonSerializer.Serialize(writer, k, options);
            JsonSerializer.Serialize(writer, id, options);
        }
        writer.WriteEndArray();
    }
}