using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;

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
        return services.AddSingleton<IHttpDownloader, SimpleHttpDownloader>();
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
            .AddSingleton<IHttpDownloader, AdvancedHttpDownloader>();
    }
}
