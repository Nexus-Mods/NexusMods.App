using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.RedEngine.ModInstallers;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddGame<Cyberpunk2077Game>()
            .AddSingleton<IModInstaller, SimpleOverlayModInstaller>()
            .AddSingleton<IModInstaller, FolderlessModInstaller>()
            .AddSingleton<IModInstaller, AppearancePreset>()
            .AddSingleton<IModInstaller, RedModInstaller>()
            .AddSingleton<ITool, RunGameTool<Cyberpunk2077Game>>()
            .AddSingleton<ITool, RedModDeployTool>()

            // Diagnostics
            .AddSingleton<CyberEngineTweaksMissingDiagnosticEmitter>()
            .AddSingleton<Red4ExtMissingDiagnosticEmitter>()
            
            .AddSettings<Cyberpunk2077Settings>();
        return services;
    }
}
