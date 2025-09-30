using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Converter factory for <see cref="Optional{T}"/>
/// </summary>
[UsedImplicitly]
public class OptionalConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var typeArguments = typeToConvert.GetGenericArguments();
        var valueType = typeArguments[0];

        var converter = (JsonConverter)Activator.CreateInstance(
            type: typeof(OptionalConverter<>).MakeGenericType(valueType),
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: [options],
            culture: null
        )!;

        return converter;
    }

    private class OptionalConverter<TValue> : JsonConverter<Optional<TValue>>
        where TValue : notnull
    {
        private readonly JsonConverter<TValue> _valueConverter;

        [UsedImplicitly]
        public OptionalConverter(JsonSerializerOptions options)
        {
            _valueConverter = (JsonConverter<TValue>)options.GetConverter(typeof(TValue));
        }

        public override Optional<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return Optional<TValue>.None;
            var value = _valueConverter.Read(ref reader, typeof(TValue), options);
            return Optional<TValue>.Create(value);
        }

        public override void Write(Utf8JsonWriter writer, Optional<TValue> value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
            }
            else
            {
                _valueConverter.Write(writer, value.Value, options);
            }
        }
    }
}
