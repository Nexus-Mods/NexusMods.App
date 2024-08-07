using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddDownloadModels(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddPersistedJobStateModel();
    }
    
    /// <summary>
    /// Registers the <typeparamref name="T"/> as a singleton and <see cref="IPersistedJobWorker"/> if applicable.
    /// </summary>
    public static IServiceCollection AddWorker<T>(this IServiceCollection serviceCollection)
        where T : class, IJobWorker
    {
        if (typeof(IPersistedJobWorker).IsAssignableFrom(typeof(T)))
        {
            serviceCollection
                .AddSingleton<IPersistedJobWorker>(s => (IPersistedJobWorker)s.GetRequiredService<T>());
        }

        return serviceCollection.AddSingleton<T>();
    }
}
