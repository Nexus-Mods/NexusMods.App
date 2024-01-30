using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.BethesdaGameStudios.SkyrimLegendaryEdition;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services) =>
        services.AddGame<SkyrimSpecialEdition.SkyrimSpecialEdition>()
            .AddGame<SkyrimLegendaryEdition.SkyrimLegendaryEdition>()
            .AddSingleton<ITool, SkyrimLegendaryEditionGameTool>()
            .AddSingleton<ITool, SkyrimSpecialEditionGameTool>()
            .AddSingleton<PluginAnalyzer>()
            .AddAllSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<PluginSorter>();
}
