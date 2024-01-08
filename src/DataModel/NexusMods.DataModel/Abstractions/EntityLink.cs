using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;

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
    public IId Id { get; }

    /// <summary>
    /// Retrieves the item from the data store.
    /// </summary>
    [JsonIgnore]
    public T? Value => Get();

    [JsonIgnore]
    private readonly IDataStore _store;

    /// <summary/>
    /// <param name="id">The ID to link to within the database.</param>
    /// <param name="store">The store in which the link will be held.</param>
    public EntityLink(IId id, IDataStore store)
    {
        Id = id;
        _store = store;
    }

    private T? Get()
    {
        _value ??= _store.Get<T>(Id);
        return _value;
    }

    /// <inheritdoc />
    public static EntityLink<T> Empty(IDataStore store) => new(IdEmpty.Empty, store);

    /// <inheritdoc />
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return Id is IdEmpty ? initial : visitor(initial, Value!);
    }

    /// <summary/>
    public static implicit operator T(EntityLink<T> t) => t.Value!;
}
