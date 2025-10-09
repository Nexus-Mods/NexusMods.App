using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Jobs;

public static class Services
{
    public static IServiceCollection AddJobMonitor(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IJobMonitor, JobMonitor>();
    }
}
