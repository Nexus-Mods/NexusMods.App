using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS8604

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Provides a table abstraction over an underlying Data Store (<see cref="IDataStore"/>)
/// which allows you to interface with values in the store using Key-Value semantics.
/// </summary>
[JsonConverter(typeof(EntityDictionaryConverterFactory))]
public struct EntityDictionary<TK, TV> :
    IEmptyWithDataStore<EntityDictionary<TK, TV>>,
    IEnumerable<KeyValuePair<TK, TV>>,
    IWalkable<Entity>
    where TV : Entity where TK : notnull
{
    // Note: Items are currently persisted through calls to .DataStoreId ; but this will probably be improved.

    private readonly ImmutableDictionary<TK, Id> _coll;
    private readonly IDataStore _store;

    /// <summary>
    /// Initializes a dictionary of entities backing.
    /// </summary>
    /// <param name="store">Abstraction over where the data will be held.</param>
    public EntityDictionary(IDataStore store)
    {
        _store = store;
        _coll = ImmutableDictionary<TK, Id>.Empty;
    }
    /// <summary>
    /// Initializes a dictionary of entities backed by a store.
    /// </summary>
    /// <param name="store">Abstraction over where the data will be held.</param>
    /// <param name="kvs">Initial set of keys and values from which to create the collection from.</param>
    public EntityDictionary(IDataStore store, IEnumerable<KeyValuePair<TK, Id>> kvs)
    {
        _store = store;
        _coll = ImmutableDictionary.CreateRange(kvs);
    }

    /// <summary>
    /// Retrieves a key from this dictionary.
    /// </summary>
    public TV this[TK k] => _store.Get<TV>(_coll[k])!;

    /// <summary>
    /// Returns the key-value pairs in this dictionary.
    /// </summary>
    public IEnumerable<KeyValuePair<TK, Id>> Ids => _coll;

    /// <summary>
    /// Number of elements in this dictionary.
    /// </summary>
    public int Count => _coll.Count;

    /// <summary>
    /// Returns the values in this dictionary.
    /// </summary>
    public IEnumerable<TV> Values => GetValues();

    /// <summary>
    /// Returns the keys from this dictionary.
    /// </summary>
    public IEnumerable<TK> Keys => _coll.Keys;

    /// <summary>
    /// Adds the (key,value) tuple to the collection.
    /// </summary>
    /// <param name="key">Key used in the collection.</param>
    /// <param name="val">Value to add to the collection.</param>
    /// <returns>A new dictionary with the item present in its collection.</returns>
    public EntityDictionary<TK, TV> With(TK key, TV val)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.SetItem(key, val.DataStoreId));
    }

    /// <summary>
    /// Adds val to the collection using keyFn(val) as the key
    /// </summary>
    /// <param name="keyFn">Function used to retrieve the key.</param>
    /// <param name="val">Value to add to the collection.</param>
    /// <returns>A new dictionary with the item present in its collection.</returns>
    public EntityDictionary<TK, TV> With(TV val, Func<TV, TK> keyFn)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.SetItem(keyFn(val), val.DataStoreId));
    }

    /// <summary>
    /// Adds a group of values to the collection, using <paramref name="keyFn"/> to extract
    /// the keys.
    /// </summary>
    /// <param name="keyFn">Function used to retrieve the key.</param>
    /// <param name="val">Values to add to the collection.</param>
    /// <returns>A new dictionary with the item present in its collection.</returns>
    public EntityDictionary<TK, TV> With(IEnumerable<TV> val, Func<TV, TK> keyFn)
    {
        var builder = _coll.ToBuilder();
        foreach (var v in val)
            builder[keyFn(v)] = v.DataStoreId;

        return new EntityDictionary<TK, TV>(_store, builder.ToImmutable());
    }

    /// <summary>
    /// Removes a key from the collection; returning a new dictionary.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A new dictionary with the item present in its collection.</returns>
    public EntityDictionary<TK, TV> Without(TK key)
    {
        return new EntityDictionary<TK, TV>(_store, _coll.Remove(key));
    }

    /// <summary>
    /// Checks if the dictionary contains a certain key.
    /// </summary>
    /// <param name="val">The key to check.</param>
    /// <returns>True if the collection contains this key, else false.</returns>
    public bool ContainsKey(TK val)
    {
        return _coll.ContainsKey(val);
    }

    /// <inheritdoc />
    public static EntityDictionary<TK, TV> Empty(IDataStore store) => new(store);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
    {
        foreach (var (key, value) in _coll)
            yield return KeyValuePair.Create(key, _store.Get<TV>(value)!);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Transforms a value found at at given id with a given function.
    /// If the function returns null, the value is removed from the dictionary.
    /// </summary>
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
    /// <param name="func">
    ///     Function used to determine if a value should be kept.
    ///     Returning a null value discards the item.
    ///     Returning the original value or a new one will keep it.
    /// </param>
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

    /// <inheritdoc />
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return Values.Aggregate(initial, (state, itm) =>
        {
            state = visitor(state, itm);
            return itm.Walk(visitor, state);
        });
    }

    private IEnumerable<TV> GetValues()
    {
        foreach (var id in _coll.Values)
            yield return _store.Get<TV>(id)!;
    }
}

/// <summary>
/// Factory used to convert dictionaries to/from database with support for
/// dependency injection.
/// </summary>
public class EntityDictionaryConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _provider;

    /// <inheritdoc />
    public EntityDictionaryConverterFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 2 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityDictionary<,>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_provider.GetRequiredService(typeof(EntityDictionaryConverter<,>).MakeGenericType(typeToConvert.GenericTypeArguments));
    }
}

/// <summary>
/// Converter used to serialize and de(serialize) entity dictionaries to/from JSON.
/// </summary>
public class EntityDictionaryConverter<TK, TV> : JsonConverter<EntityDictionary<TK, TV>>
    where TV : Entity where TK : notnull
{
    private readonly IDataStore _store;

    /// <inheritdoc />
    public EntityDictionaryConverter(IDataStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
