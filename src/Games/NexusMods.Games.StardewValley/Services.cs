using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.StardewValley.Emitters;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, StardewValley>()
            .AddSingleton<ILoadoutDiagnosticEmitter, MissingDependenciesEmitter>()
            .AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
