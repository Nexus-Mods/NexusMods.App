using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Activities;
using NexusMods.Extensions.DependencyInjection;

namespace NexusMods.Activities;

/// <summary>
/// Adds activity related services to your dependency injection container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the <see cref="IActivityMonitor"/> to your dependency injection container.
    /// </summary>
    public static IServiceCollection AddActivityMonitor(this IServiceCollection coll)
    {
        coll.AddAllSingleton<IActivityFactory, IActivityMonitor, ActivityMonitor>();
        return coll;
    }
}
