using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, SkyrimSpecialEdition>();
        services.AddAllSingleton<IGame, SkyrimLegendaryEdition>();
        services.AddSingleton<ITool, SkyrimLegendaryEditionGameTool>();
        services.AddSingleton<ITool, SkyrimSpecialEditionGameTool>();
        services.AddAllSingleton<IFileAnalyzer, PluginAnalyzer>();
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }

}
