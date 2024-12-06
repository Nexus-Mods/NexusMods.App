using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.FileHashes.HashValues;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Extensions methods for the services.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the file hashes verbs to the service collection.
    /// </summary>
    public static IServiceCollection AddHashSerializers(this IServiceCollection services)
    {
        services.AddSingleton<JsonConverter, HashJsonConverter>();
        return services;
    }
    
}
