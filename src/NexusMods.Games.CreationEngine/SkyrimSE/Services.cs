using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public static class Services
{
    public static IServiceCollection AddCreationEngine(this IServiceCollection services)
    {
        services.AddGame<SkyrimSE>();

        return services;
    }
}
