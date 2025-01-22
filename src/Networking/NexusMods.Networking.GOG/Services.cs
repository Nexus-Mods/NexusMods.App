using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Networking.GOG.CLI;
using NexusMods.Networking.GOG.Models;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.GOG;

public static class Services
{
    public static IServiceCollection AddGOG(this IServiceCollection services)
    {
        services.AddGOGVerbs();
        services.AddSingleton<IClient, Client>();
        services.AddAuthInfoModel();
        services.AddOptionParser(s => ProductId.From(ulong.Parse(s)));
        return services;
    }
}
