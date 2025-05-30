using Microsoft.Extensions.DependencyInjection;
using NexusMods.Backend.Stores.EpicGameStore.Models;

namespace NexusMods.Backend.Stores.EpicGameStore;

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
