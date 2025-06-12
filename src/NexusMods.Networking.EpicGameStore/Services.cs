using Microsoft.Extensions.DependencyInjection;
using NexusMods.Networking.EpicGameStore.Models;

namespace NexusMods.Networking.EpicGameStore;

public static class Services
{
    public static IServiceCollection AddEpicGameStore(this IServiceCollection s)
    {
        s.AddSingleton<EgDataClient>();
        s.AddEpicGameStoreBuildModel(); 
        s.AddEGSVerbs();
        return s;
    }
}
