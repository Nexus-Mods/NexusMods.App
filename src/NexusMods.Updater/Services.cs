using Microsoft.Extensions.DependencyInjection;
using NexusMods.Updater.DownloadSources;

namespace NexusMods.Updater;

public static class Services
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        return services.AddSingleton<UpdaterService>()
            .AddSingleton<Github>();
    }

}
