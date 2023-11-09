using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services) =>
        services.AddAllSingleton<IGame, SkyrimSpecialEdition>()
            .AddAllSingleton<IGame, SkyrimLegendaryEdition>()
            .AddSingleton<ITool, SkyrimLegendaryEditionGameTool>()
            .AddSingleton<ITool, SkyrimSpecialEditionGameTool>()
            .AddSingleton<PluginAnalyzer>()
            .AddAllSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<PluginSorter>();
}
