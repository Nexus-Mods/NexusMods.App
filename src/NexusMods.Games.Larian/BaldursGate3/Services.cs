using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.Larian.BaldursGate3.Emitters;
using NexusMods.Games.Larian.BaldursGate3.RunGameTools;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Services
{
    public static IServiceCollection AddBaldursGate3(this IServiceCollection services)
    {
        services
            .AddGame<BaldursGate3>()
            .AddSingleton<ITool, BG3RunGameTool>()
            .AddSettings<BaldursGate3Settings>()
            .AddPipelines()
            // diagnostics
            .AddSingleton<DependencyDiagnosticEmitter>();

        return services;
    }
}
