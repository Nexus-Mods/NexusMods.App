using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Sorting;

/// <summary>
/// A marker interface for rules used in sorting
/// </summary>
/// <typeparam name="TType"></typeparam>
/// <typeparam name="TId"></typeparam>
[JsonDerivedType(typeof(First<,>))]
[JsonDerivedType(typeof(After<,>))]
[JsonDerivedType(typeof(Before<,>))]
public interface ISortRule<TType, TId> 
where TType : IHasEntityId<TId>
{
    
}


public class ISortRuleConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _services;

    public ISortRuleConverterFactory(IServiceProvider services)
    {
        _services = services;
    }
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.GenericTypeArguments.Length == 2 &&
        typeToConvert.GetGenericTypeDefinition() == typeof(ISortRule<,>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)_services.GetRequiredService(typeof(ISortRuleConverter<,>).MakeGenericType(typeToConvert.GetGenericArguments()));
    }
}

public class ISortRuleConverter<TParent, TId> : JsonConverter<ISortRule<TParent, TId>>
    where TParent : IHasEntityId<TId>
{
    public override ISortRule<TParent, TId>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ISortRule<TParent, TId>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, ISortRule<TParent, TId> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(value, options);
    }
}