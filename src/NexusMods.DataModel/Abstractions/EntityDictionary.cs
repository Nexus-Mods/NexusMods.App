using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.ModLists;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(EntityDictionaryConverterFactory))]
public struct EntityDictionary<TK, TV>
where TV : Entity where TK : notnull
{
    private readonly ImmutableDictionary<TK, Id> _coll;
    private readonly IDataStore _store;


    public EntityDictionary(IDataStore? store = null)
    {
        _store = store ?? IDataStore.CurrentStore.Value!;
        if (store == null)
            throw new NoDataStoreException();
        _coll = ImmutableDictionary<TK, Id>.Empty;
    }

    private EntityDictionary(IDataStore store, ImmutableDictionary<TK, Id> coll)
    {
        _store = store;
        _coll = coll;
    }

    public EntityDictionary(IEnumerable<KeyValuePair<TK, Id>> kvs)
    {
        _coll = ImmutableDictionary.CreateRange(kvs);
    }

    public EntityDictionary<TK, TV> With(TK key, TV val)
    {
        return new EntityDictionary<TK, TV>(_coll.SetItem(key, val.Id));
    }

    public EntityDictionary<TK, TV> Without(TK key)
    {
        return new EntityDictionary<TK, TV>(_coll.Remove(key));
    }

    public bool ContainsKey(TK val)
    {
        return _coll.ContainsKey(val);
    }

    public IEnumerable<KeyValuePair<TK, Id>> Ids => _coll;
    public int Count => _coll.Count;

    public static EntityDictionary<string,ModList> Empty()
    {
        return new EntityDictionary<string, ModList>(IDataStore.CurrentStore.Value);
    }
}

public class EntityDictionaryConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 2 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityDictionary<,>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(EntityDictionaryConverter<,>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

public class EntityDictionaryConverter<TK, TV> : JsonConverter<EntityDictionary<TK, TV>>
    where TV : Entity where TK : notnull
{
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
        
        return new EntityDictionary<TK, TV>(lst);
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