using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Json;
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

        services.AddSingleton<JsonConverter, AbstractClassConverterFactory<IMetadata>>();
        services.AddSingleton<JsonConverter, EntityIdConverter>();

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
