using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Extensions.DependencyInjection;

namespace NexusMods.Games.Sifu;

public static class Services
{
    public static IServiceCollection AddSifu(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddAllSingleton<IGame, Sifu>();
        serviceCollection.AddAllSingleton<IModInstaller, SifuModInstaller>();
        return serviceCollection;
    }

}
