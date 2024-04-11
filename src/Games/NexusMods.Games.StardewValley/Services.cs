using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.WebAPI;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services
            .AddGame<StardewValley>()
            .AddSingleton<ITool, RunGameTool<StardewValley>>()

            // Installers
            .AddSingleton<SMAPIInstaller>()
            .AddSingleton<SMAPIModInstaller>()

            // Diagnostics
            .AddSingleton<DependencyDiagnosticEmitter>()
            .AddSingleton<MissingSMAPIEmitter>()
            .AddSingleton<SMAPIModDatabaseCompatibilityDiagnosticEmitter>()
            .AddSingleton<SMAPIGameVersionDiagnosticEmitter>()

            // Misc
            .AddSingleton<ISMAPIWebApi, SMAPIWebApi>()
            .AddSingleton<ITypeFinder, TypeFinder>();

        return services;
    }
}
