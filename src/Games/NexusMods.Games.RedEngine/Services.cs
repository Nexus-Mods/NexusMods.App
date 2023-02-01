using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, Cyberpunk2077>();
        services.AddSingleton<IModInstaller, SimpleOverlyModInstaller>();
        services.AddSingleton<ITool, RunGameTool<Cyberpunk2077>>();
        services.AddSingleton<ITool, RedModDeployTool>();
        return services;
    }
}