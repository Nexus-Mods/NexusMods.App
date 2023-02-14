using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.HttpDownloader.Verbs;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

public static class Services
{
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services.AddSingleton<IHttpDownloader, SimpleHttpDownloader>()
            .AddVerb<DownloadUri>()
            .AddAllSingleton<IResource, IResource<IHttpDownloader, Size>>(s => new Resource<IHttpDownloader, Size>("Downloads"));
    }

    public static IServiceCollection AddAdvancedHttpDownloader(this IServiceCollection services)
    {
        return services.AddSingleton<IHttpDownloader, AdvancedHttpDownloader>()
            .AddVerb<DownloadUri>()
            .AddAllSingleton<IResource, IResource<IHttpDownloader, Size>>(s => new Resource<IHttpDownloader, Size>("Downloads"));
    }
}