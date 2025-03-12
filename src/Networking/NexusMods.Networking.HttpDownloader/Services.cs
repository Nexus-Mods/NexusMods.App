using Microsoft.Extensions.DependencyInjection;

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
            client.DefaultRequestHeaders.Add("User-Agent", "NexusMods.App");
            return client;
        });
    }
}
