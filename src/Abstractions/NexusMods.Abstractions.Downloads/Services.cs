using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddNexusModsLibraryAttributes(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddPersistedDownloadStateModel();
    }
}
