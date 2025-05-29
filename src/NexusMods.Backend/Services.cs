using Microsoft.Extensions.DependencyInjection;
using NexusMods.Backend.Stores.EpicGameStore;

namespace NexusMods.Backend;

public static class Services
{
    public static IServiceCollection AddBackendServices(this IServiceCollection s)
    {
        s.AddEpicGameStore();
        return s;
    }
    
}
