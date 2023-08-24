using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, StardewValley>()
            .AddSingleton<IFileAnalyzer, SMAPIManifestAnalyzer>()
            .AddSingleton<SMAPIInstaller>()
            .AddSingleton<SMAPIModInstaller>()
            .AddSingleton<ILoadoutDiagnosticEmitter, MissingDependenciesEmitter>()
            .AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
