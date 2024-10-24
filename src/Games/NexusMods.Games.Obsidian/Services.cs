using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.Obsidian;

public static class Services
{
    public static IServiceCollection AddObsidianGames(this IServiceCollection services)
    {
        services.AddGame<FalloutNewVegas.FalloutNewVegas>()
            .AddSingleton<ITool, RunGameTool<FalloutNewVegas.FalloutNewVegas>>()
            .AddSettings<FalloutNewVegas.FalloutNewVegasSettings>();
        return services;
    }
}
