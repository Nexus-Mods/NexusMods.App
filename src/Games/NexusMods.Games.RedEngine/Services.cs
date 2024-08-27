using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddGame<Cyberpunk2077Game>()
            .AddRedModLoadoutGroupModel()
            .AddRedModInfoFileModel()
            .AddSingleton<ITool, RunGameTool<Cyberpunk2077Game>>()
            .AddSingleton<ITool, RedModDeployTool>()

            // Diagnostics
            
            
            .AddSettings<Cyberpunk2077Settings>();
        return services;
    }
}
