using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.BuildInfo;

namespace NexusMods.Networking.HttpDownloader;

public static class Services
{
    /// <summary>
    /// Add the default HTTP downloader services
    /// </summary>
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ApplicationConstants.UserAgent);
            return client;
        });
    }
}
