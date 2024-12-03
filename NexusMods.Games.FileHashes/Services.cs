using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.FileHashes.CLI;
using NexusMods.Games.FileHashes.HashValues;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Games.FileHashes;

/// <summary>
/// Extensions methods for the services.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the file hashes verbs to the service collection.
    /// </summary>
    public static IServiceCollection AddFileHashes(this IServiceCollection services)
    {
        services.AddFileHashesVerbs();
        services.AddSingleton<JsonConverter, HashJsonConverter>();
        return services;
    }
    
}
