using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;

namespace NexusMods.Games.CreationEngine;

public static class Services
{
    public static IServiceCollection AddCreationEngine(this IServiceCollection services)
    {
        services.AddGame<SkyrimSE.SkyrimSE>();
        services.AddGame<Fallout4.Fallout4>();

        return services;
    }
}
