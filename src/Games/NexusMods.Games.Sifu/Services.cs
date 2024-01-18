using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;

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
