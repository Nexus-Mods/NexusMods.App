using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;

namespace NexusMods.Library;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddLibrary(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ILibraryService, LibraryService>()
            .AddSingleton<AddLibraryFileJobWorker>()
            .AddSingleton<AddLocalFileJobWorker>()
            .AddSingleton<ExtractArchiveJobWorker>()
            .AddSingleton<InstallLoadoutItemJobWorker>()
            .AddSingleton<AddDownloadJobWorker>();
    }
}
