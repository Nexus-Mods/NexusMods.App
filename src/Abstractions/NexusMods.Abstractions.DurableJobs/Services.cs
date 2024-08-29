using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// Service extensions for durable jobs.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the given durable job to the service collection.
    /// </summary>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services)
        where TJob : AJob
    {
        services.AddSingleton<AJob, TJob>();
        services.AddSingleton<JsonConverter, JobSerializer>();
        return services;
    }
    
}
