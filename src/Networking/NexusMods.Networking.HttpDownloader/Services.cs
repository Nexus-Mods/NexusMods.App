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
            .AddSingleton<IHttpDownloader, SimpleHttpDownloader>()
            .AddSettings<HttpDownloaderSettings>()
            .AddWorker<HttpDownloadJobWorker>()
            .AddHttpDownloadJobPersistedStateModel();
    }
}
