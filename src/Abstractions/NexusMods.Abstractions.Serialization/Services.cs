using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Converters;
using NexusMods.Abstractions.Serialization.Json;
using NexusMods.Abstractions.Serialization.Settings;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Serialization;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Added the 'base entities' for the DataModel; building blocks used to create other entities.
    /// </summary>
    public static IServiceCollection AddSerializationAbstractions(this IServiceCollection services)
    {
        services.AddSettingsStorageBackend<JsonStorageBackend>();

        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<Entity>>();
        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<IMetadata>>();

        services.AddSingleton<JsonConverter, EntityHashSetConverterFactory>();
        services.AddSingleton(typeof(EntityHashSetConverter<>));
        services.AddSingleton<JsonConverter, EntityDictionaryConverterFactory>();
        services.AddSingleton(typeof(EntityDictionaryConverter<,>));
        services.AddSingleton<JsonConverter, EntityLinkConverterFactory>();
        services.AddSingleton(typeof(EntityLinkConverter<>));
        services.AddSingleton<JsonConverter, IdJsonConverter>();

        services.AddSingleton(s =>
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new JsonStringEnumConverter());
            foreach (var converter in s.GetServices<JsonConverter>())
                opts.Converters.Add(converter);
            return opts;
        });

        return services;
    }
}
