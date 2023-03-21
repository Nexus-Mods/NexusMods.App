using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.HttpDownloader.Verbs;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Services you can add to your DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the <see cref="SimpleHttpDownloader"/> to your dependency injection container
    /// as the default <see cref="IHttpDownloader"/> implementation.
    /// </summary>
    /// <param name="services">Your DI container collection builder.</param>
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services.AddSingleton<IHttpDownloader, SimpleHttpDownloader>()
            .AddVerb<DownloadUri>()
            .AddAllSingleton<IResource, IResource<IHttpDownloader, Size>>(_ => new Resource<IHttpDownloader, Size>("Downloads"));
    }

    /// <summary>
    /// Adds the <see cref="AdvancedHttpDownloader"/> to your dependency injection container
    /// as the default <see cref="IHttpDownloader"/> implementation.
    /// </summary>
    /// <param name="services">Your DI container collection builder.</param>
    /// <param name="settings">Settings for the HTTP downloader.</param>
    public static IServiceCollection AddAdvancedHttpDownloader(this IServiceCollection services, IHttpDownloaderSettings? settings = null)
    {
        settings ??= new HttpDownloaderSettings();
        return services.AddSingleton(settings)
            .AddSingleton<IHttpDownloader, AdvancedHttpDownloader>()
            .AddVerb<DownloadUri>()
            .AddAllSingleton<IResource, IResource<IHttpDownloader, Size>>(_ => new Resource<IHttpDownloader, Size>("Downloads"));
    }
}
