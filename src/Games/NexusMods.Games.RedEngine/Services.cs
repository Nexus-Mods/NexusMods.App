using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.RedEngine.ModInstallers;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddGame<Cyberpunk2077>()
            .AddSingleton<IModInstaller, SimpleOverlayModInstaller>()
            .AddSingleton<IModInstaller, FolderlessModInstaller>()
            .AddSingleton<IModInstaller, AppearancePreset>()
            .AddSingleton<IModInstaller, RedModInstaller>()
            .AddSingleton<ITool, RunGameTool<Cyberpunk2077>>()
            .AddSingleton<ITool, RedModDeployTool>()
            .AddSettings<Cyberpunk2077Settings>();
        return services;
    }
}
