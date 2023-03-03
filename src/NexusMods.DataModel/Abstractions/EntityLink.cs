using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// This record holds a reference to another entity in the data store.
/// This is essentially a wrapper around an ID.
/// </summary>
/// <typeparam name="T">Type of entity this entity links to </typeparam>
[JsonConverter(typeof(EntityLinkConverterFactory))]
public record struct EntityLink<T> : IEmptyWithDataStore<EntityLink<T>>,
    IWalkable<Entity>
    where T : Entity
{
    [JsonIgnore]
    private T? _value;

    /// <summary>
    /// ID of the element in the data store.
    /// </summary>
    public Id Id { get; }

    /// <summary>
    /// Retrieves the item from the data store.
    /// </summary>
    [JsonIgnore]
    public T Value => Get();

    [JsonIgnore]
    private readonly IDataStore _store;

    /// <summary/>
    /// <param name="id">The ID to link to within the database.</param>
    /// <param name="store">The store in which the link will be held.</param>
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

    /// <inheritdoc />
    public static EntityLink<T> Empty(IDataStore store) => new(IdEmpty.Empty, store);

    /// <inheritdoc />
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return Id is IdEmpty ? initial : visitor(initial, Value);
    }

    /// <summary/>
    public static implicit operator T(EntityLink<T> t) => t.Value;

    /// <summary/>
    public static implicit operator EntityLink<T>(T t) => new(t.DataStoreId, t.Store);
}

/// <inheritdoc />
public class EntityLinkConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _provider;

    /// <inheritdoc />
    public EntityLinkConverterFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityLink<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_provider.GetService(typeof(EntityLinkConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

/// <inheritdoc />
public class EntityLinkConverter<T> : JsonConverter<EntityLink<T>>
    where T : Entity
{
    private readonly IDataStore _store;

    /// <inheritdoc />
    public EntityLinkConverter(IDataStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override EntityLink<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = JsonSerializer.Deserialize<Id>(ref reader, options)!;
        return new EntityLink<T>(id, _store);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityLink<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Id, options);
    }
}
