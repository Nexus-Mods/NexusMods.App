using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.DataModel.Abstractions;

public struct EntityHashSet<T> : IEmptyWithDataStore<EntityHashSet<T>>
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

    private EntityHashSet(ImmutableHashSet<Id> coll)
    {
        _coll = coll;
    }

    public EntityHashSet(IEnumerable<Id> ids)
    {
        _coll = ImmutableHashSet.CreateRange(ids);
    }

    public EntityHashSet<T> With(T val)
    {
        return new EntityHashSet<T>(_coll.Add(val.Id));
    }

    public EntityHashSet<T> Without(T val)
    {
        return new EntityHashSet<T>(_coll.Remove(val.Id));
    }

    public bool Contains(T val)
    {
        return _coll.Contains(val.Id);
    }

    public IEnumerable<Id> Ids => _coll;
    public int Count => _coll.Count;
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
            lst.Add(JsonSerializer.Deserialize<Id>(ref reader, options));
            reader.Read();
        }
        
        return new EntityHashSet<T>(lst);
    }

    public override void Write(Utf8JsonWriter writer, EntityHashSet<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var itm in value.Ids)
            JsonSerializer.Serialize(writer, itm, options);
        writer.WriteEndArray();
    }
}