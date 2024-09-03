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
        return services;
    }
    
    /// <summary>
    /// Adds the given unit of work job to the service collection.
    /// </summary>
    public static IServiceCollection AddUnitOfWorkJob<TJob>(this IServiceCollection services)
        where TJob : AUnitOfWork
    {
        services.AddSingleton<AUnitOfWork, TJob>();
        return services;
    }
    
}
