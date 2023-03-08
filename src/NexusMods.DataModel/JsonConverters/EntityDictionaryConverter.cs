using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.JsonConverters;

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

        var lst = new List<KeyValuePair<TK, IId>>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var tk = JsonSerializer.Deserialize<TK>(ref reader, options)!;
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                throw new JsonException("Found end of array when expecting dictionary value");

            var tv = JsonSerializer.Deserialize<IId>(ref reader, options);
            reader.Read();
            lst.Add(new KeyValuePair<TK, IId>(tk, tv!));
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
