using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.JsonConverters;

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
        var id = JsonSerializer.Deserialize<IId>(ref reader, options)!;
        return new EntityLink<T>(id, _store);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityLink<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Id, options);
    }
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
