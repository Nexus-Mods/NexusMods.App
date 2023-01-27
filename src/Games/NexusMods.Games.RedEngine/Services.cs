using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngine(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, Cyberpunk2077>();
        return services;
    }
}