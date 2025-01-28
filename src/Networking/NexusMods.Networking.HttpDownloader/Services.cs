using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Networking.HttpDownloader;

public static class Services
{
    /// <summary>
    /// Add the default HTTP downloader services
    /// </summary>
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services
            .AddSettings<HttpDownloaderSettings>()
            .AddSingleton<HttpClient>(s =>
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "NexusMods.App");
                    return client;
                }
            );
    }
}
