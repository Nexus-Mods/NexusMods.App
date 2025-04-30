using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

#pragma warning disable CS1591

namespace NexusMods.Abstractions.Serialization.Json;

public class AbstractClassConverterFactory<T> : JsonConverterFactory
{
    private readonly Type _type;
    private readonly IServiceProvider _provider;

    public AbstractClassConverterFactory(IServiceProvider provider)
    {
        _provider = provider;
        _type = typeof(T);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsAssignableTo(_type);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsAbstract || typeToConvert.IsInterface)
        {
            return (JsonConverter?)Activator.CreateInstance(typeof(AbstractClassConverterGenerator<>)
                .MakeGenericType(typeToConvert), _provider);
        }
        return (JsonConverter?)Activator.CreateInstance(typeof(ConcreteConverterGenerator<>)
            .MakeGenericType(typeToConvert), _provider);
    }
}

public class AbstractClassConverterGenerator<T> : JsonConverter<T>
{
    private readonly IServiceProvider _provider;
    private readonly Type _type;
    private readonly Dictionary<string, Type> _registry;

    public AbstractClassConverterGenerator(IServiceProvider provider)
    {
        _provider = provider;
        _type = typeof(T);

        _registry = new Dictionary<string, Type>();

        foreach (var type in GetSubClasses())
        {
            var nameAttr = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonNameAttribute))
                .Select(t => (string)t.ConstructorArguments.First().Value!)
                .FirstOrDefault();

            if (nameAttr == default)
                throw new JsonException($"Type {type} of interface {_type} does not have a JsonNameAttribute");
            _registry[nameAttr] = type;

            var aliases = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonAliasAttribute))
                .Select(t => t.ConstructorArguments.First());

            foreach (var alias in aliases)
                _registry[(string)alias.Value!] = type;
        }
    }

    private IEnumerable<Type> GetSubClasses()
    {
        var finders = _provider.GetRequiredService<IEnumerable<ITypeFinder>>();
        return finders.SelectMany(f => f.DescendentsOf(_type))
            .Where(d => !d.IsAbstract && !d.IsInterface)
            .Distinct();
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readAhead = reader;
        if (readAhead.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");
        readAhead.Read();
        if (readAhead.GetString() != "$type")
            throw new JsonException("Expected $type as first property on object");
        readAhead.Read();
        var typeName = readAhead.GetString()!;
        if (_registry.TryGetValue(typeName, out var type))
        {
            return (T?)JsonSerializer.Deserialize(ref reader, type, options);
        }
        throw new JsonException("Unknown type " + typeName);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value!.GetType(), options);
    }
}
