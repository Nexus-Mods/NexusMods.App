using Microsoft.Extensions.DependencyInjection;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// Services you can add to your DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the networking services to your dependency injection container.
    /// </summary>
    /// <param name="services">Your DI container collection builder.</param>
    public static IServiceCollection AddDownloaders(this IServiceCollection services)
    {
        return services.AddAllSingleton<IDownloadService, DownloadService>()
            .AddTransient<NxmDownloadTask>()
            .AddTransient<HttpDownloadTask>()
            .AddAttributeCollection(typeof(DownloaderState))
            .AddAttributeCollection(typeof(HttpDownloadState))
            .AddAttributeCollection(typeof(NxmDownloadState));
    }
}
