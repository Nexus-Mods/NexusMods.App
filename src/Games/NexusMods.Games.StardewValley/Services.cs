using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services
            .AddGame<StardewValley>()
            .AddSingleton<ITool, RunGameTool<StardewValley>>()
            .AddSingleton<ILoadoutDiagnosticEmitter, DependencyDiagnosticEmitter>()
            .AddSingleton<ILoadoutDiagnosticEmitter, MissingSMAPIEmitter>()
            .AddSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<SMAPIInstaller>();

        return services;
    }
}
