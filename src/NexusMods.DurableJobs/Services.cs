using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

public static class Services
{
    public static IServiceCollection AddDurableJobs(this IServiceCollection services)
    {
        services.AddSingleton<IJobManager, JobManager>();
        return services;
    }
    
    public static IServiceCollection AddInMemoryJobStore(this IServiceCollection services)
    {
        services.AddSingleton<IJobStateStore, InMemoryJobStore>();
        return services;
    }
}
