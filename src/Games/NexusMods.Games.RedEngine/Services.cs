using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.RedEngine.ModInstallers;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, Cyberpunk2077>();
        services.AddSingleton<IModInstaller, SimpleOverlayModInstaller>();
        services.AddSingleton<IModInstaller, FolderlessModInstaller>();
        services.AddSingleton<IModInstaller, AppearancePreset>();
        services.AddSingleton<IModInstaller, RedModInstaller>();
        services.AddSingleton<ITool, RunGameTool<Cyberpunk2077>>();
        services.AddSingleton<ITool, RedModDeployTool>();
        return services;
    }
}
