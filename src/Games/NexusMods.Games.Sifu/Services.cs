using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.Sifu;

public static class Services
{
    public static IServiceCollection AddSifu(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGame, Sifu>();
        serviceCollection.AddSingleton<IModInstaller, SifuModInstaller>();
        return serviceCollection;
    }

}
