using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Networking.HttpDownloader;

public static class Services
{
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services
            .AddSettings<HttpDownloaderSettings>();
    }

    /// <summary>
    /// Adds a default HttpClient to the service collection, pre-configured with a User-Agent, and other default headers.
    /// </summary>
    public static IServiceCollection AddDefaultHttpClient(this IServiceCollection services)
    {
        return services.AddSingleton<HttpClient>(s =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NexusMods App");
                return client;
            }
        );
    }
}
