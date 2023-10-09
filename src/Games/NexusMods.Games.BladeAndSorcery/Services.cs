using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.BladeAndSorcery.Installers;

namespace NexusMods.Games.BladeAndSorcery;

public static class Services
{
    public static IServiceCollection AddBladeAndSorcery(this IServiceCollection services) =>
        services.AddAllSingleton<IGame, BladeAndSorcery>()
            .AddAllSingleton<IModInstaller, BladeAndSorceryModInstaller>()
            .AddSingleton<ITool, RunGameTool<BladeAndSorcery>>()
            .AddSingleton<ITypeFinder, TypeFinder>();
}
