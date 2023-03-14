using System.Text.Json;
using System.Text.Json.Serialization;
#pragma warning disable CS1591

namespace NexusMods.DataModel.JsonConverters;

// TODO: Is this dead code?

/// <inheritdoc />
public class GenericInterfaceConverterFactory : JsonConverterFactory
{
    private readonly Type _type;
    private readonly IServiceProvider _provider;

    /// <inheritdoc />
    public GenericInterfaceConverterFactory(Type type, IServiceProvider provider)
    {
        _provider = provider;
        _type = type;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == _type)
        {
            return true;
        }

        return false;
        //return GetInheritedArgs(typeToConvert).Any();
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var factory = (JsonConverterFactory)Activator.CreateInstance(typeof(AbstractClassConverterFactory<>).MakeGenericType(typeToConvert), _provider)!;
        return factory.CreateConverter(typeToConvert, options);
    }

    public Type[] GetInheritedArgs(Type type)
    {
        foreach (var i in type.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == _type)
            {
                return i.GetGenericArguments();
            }
        }

        return Array.Empty<Type>();
    }
}
