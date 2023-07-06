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
    public static IServiceCollection AddStardewValley(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddAllSingleton<IGame, StardewValley>();
        serviceCollection.AddSingleton<IFileAnalyzer, SMAPIManifestAnalyzer>();
        serviceCollection.AddSingleton<IModInstaller, SMAPIInstaller>();
        serviceCollection.AddSingleton<IModInstaller, SMAPIModInstaller>();
        serviceCollection.AddSingleton<ILoadoutDiagnosticEmitter, MissingDependenciesEmitter>();
        serviceCollection.AddSingleton<ITypeFinder, TypeFinder>();
        return serviceCollection;
    }
}
