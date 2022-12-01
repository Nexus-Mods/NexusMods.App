using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.ModLists;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(EntityLinkConverterFactory))]
public record EntityLink <T> where T : Entity
{
    public static readonly EntityLink<ModList> Empty = new(Id.Empty, IDataStore.CurrentStore.Value);

    [JsonIgnore]
    private T? _value = null;
    public Id Id { get; }
    
    [JsonIgnore]
    public T Value => Get();

    public static implicit operator T(EntityLink<T> t) => t.Value;
    public static implicit operator EntityLink<T>(T t) => new(t.Id, t.Store);

    [JsonIgnore]
    private readonly IDataStore _store;

    public EntityLink(Id id, IDataStore? store = null)
    {
        _store = (store ?? IDataStore.CurrentStore.Value)!;
        Id = id;
    }

    private T Get()
    {
        using var _ = IDataStore.WithCurrent(_store);
        _value ??= _store.Get<T>(Id);
        return _value;
    }
}

public class EntityLinkConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityLink<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(EntityLinkConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

public class EntityLinkConverter<T> : JsonConverter<EntityLink<T>>
    where T : Entity
{
    public override EntityLink<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = JsonSerializer.Deserialize<Id>(ref reader, options);
        return new EntityLink<T>(id, IDataStore.CurrentStore.Value);
    }

    public override void Write(Utf8JsonWriter writer, EntityLink<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Id, options);
    }
}