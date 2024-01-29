using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
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
