using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.Updater.DownloadSources;
using NexusMods.Updater.Verbs;

namespace NexusMods.Updater;

public static class Services
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        return services.AddSingleton<UpdaterService>()
            .AddVerb<CopyAppToFolder>()
            .AddVerb<ForceAppUpdate>()
            .AddSingleton<Github>();
    }

}
