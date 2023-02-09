using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(EntityLinkConverterFactory))]
public record EntityLink <T> : IEmptyWithDataStore<EntityLink<T>>,
    IWalkable<Entity>
    where T : Entity
{


    [JsonIgnore]
    private T? _value = null;
    public Id Id { get; }
    
    [JsonIgnore]
    public T Value => Get();

    public static implicit operator T(EntityLink<T> t) => t.Value;
    public static implicit operator EntityLink<T>(T t) => new(t.DataStoreId, t.Store);
    
    [JsonIgnore]
    private readonly IDataStore _store;

    public EntityLink(Id id, IDataStore store)
    {
        Id = id;
        _store = store;
    }

    private T Get()
    {
        _value ??= _store.Get<T>(Id);
        return _value!;
    }

    public static EntityLink<T> Empty(IDataStore store) => new(IdEmpty.Empty, store);
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return Id is IdEmpty ? initial : visitor(initial, Value);
    }
}

public class EntityLinkConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _provider;

    public EntityLinkConverterFactory(IServiceProvider provider)
    {
        _provider = provider;
    }
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityLink<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_provider.GetService(typeof(EntityLinkConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

public class EntityLinkConverter<T> : JsonConverter<EntityLink<T>>
    where T : Entity
{
    private readonly IDataStore _store;

    public EntityLinkConverter(IDataStore store)
    {
        _store = store;
    }
    public override EntityLink<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = JsonSerializer.Deserialize<Id>(ref reader, options)!;
        return new EntityLink<T>(id, _store);
    }

    public override void Write(Utf8JsonWriter writer, EntityLink<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Id, options);
    }
}