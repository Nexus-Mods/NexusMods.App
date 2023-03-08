using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class EntityHashSetConverter<T> : JsonConverter<EntityHashSet<T>> where T : Entity
{
    private readonly Lazy<IDataStore> _store;

    /// <inheritdoc />
    public EntityHashSetConverter(IServiceProvider provider)
    {
        _store = new Lazy<IDataStore>(provider.GetRequiredService<IDataStore>());
    }

    /// <inheritdoc />
    public override EntityHashSet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");
        reader.Read();

        var lst = new List<IId>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            lst.Add(JsonSerializer.Deserialize<IId>(ref reader, options)!);
            reader.Read();
        }

        return new EntityHashSet<T>(_store.Value, lst);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityHashSet<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var itm in value.Ids)
            JsonSerializer.Serialize(writer, itm, options);

        writer.WriteEndArray();
    }
}

/// <inheritdoc />
public class EntityHashSetConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _services;

    /// <inheritdoc />
    public EntityHashSetConverterFactory(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 1 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EntityHashSet<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_services.GetRequiredService(typeof(EntityHashSetConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()));
    }
}
