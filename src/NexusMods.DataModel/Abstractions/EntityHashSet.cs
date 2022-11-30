using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.ModLists;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(EntityHashSetConverterFactory))]
public struct EntityHashSet<T>
where T : Entity
{
    public static readonly EntityHashSet<T> Empty = new EntityHashSet<T>();
    private readonly ImmutableHashSet<Id> _coll;
    public EntityHashSet()
    {
        _coll = ImmutableHashSet<Id>.Empty;
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
}

public class EntityHashSetConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityLink<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(EntityHashSetConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}

public class EntityHashSetConverter<T> : JsonConverter<EntityHashSet<T>>
    where T : Entity
{
    public override EntityHashSet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var lst = new List<Id>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            lst.Add(JsonSerializer.Deserialize<Id>(ref reader, options));
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