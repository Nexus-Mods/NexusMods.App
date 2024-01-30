using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.StardewValley.Emitters;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services.AddGame<StardewValley>()
            .AddSingleton<ILoadoutDiagnosticEmitter, MissingDependenciesEmitter>()
            .AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
