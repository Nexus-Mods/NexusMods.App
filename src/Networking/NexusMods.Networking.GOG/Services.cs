using Microsoft.Extensions.DependencyInjection;
using NexusMods.Networking.GOG.CLI;
using NexusMods.Networking.GOG.Models;

namespace NexusMods.Networking.GOG;

public static class Services
{
    public static IServiceCollection AddGOG(this IServiceCollection services)
    {
        services.AddGOGVerbs();
        services.AddSingleton<Client>();
        services.AddAuthInfoModel();
        return services;
    }
}
