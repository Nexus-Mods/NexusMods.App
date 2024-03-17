using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.BethesdaGameStudios.Fallout4;
using NexusMods.Games.BethesdaGameStudios.SkyrimLegendaryEdition;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services) =>
        services.AddGame<Fallout4.Fallout4>()
            .AddGame<SkyrimLegendaryEdition.SkyrimLegendaryEdition>()
            .AddGame<SkyrimSpecialEdition.SkyrimSpecialEdition>()
            .AddSingleton<ITool, Fallout4GameTool>()
            .AddSingleton<ITool, SkyrimLegendaryEditionGameTool>()
            .AddSingleton<ITool, SkyrimSpecialEditionGameTool>()
            .AddSingleton<PluginAnalyzer>()
            .AddAllSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<PluginSorter>();
}
