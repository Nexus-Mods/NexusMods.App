using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Emitters;
using NexusMods.Games.UnrealEngine.Installers;
using NexusMods.Games.UnrealEngine.PacificDrive;

namespace NexusMods.Games.UnrealEngine;

public static class Services
{
    public static IServiceCollection AddPacificDrive(this IServiceCollection services)
    {
        services
            .AddGame<PacificDriveGame>()
            .AddSingleton<ITool, RunGameTool<PacificDriveGame>>()

            // Misc
            .AddSettings<PacificDriveSettings>();

        return services;
    }

    public static IServiceCollection AddUnrealEngineGames(this IServiceCollection services)
    {
        services
            // Games
            .AddPacificDrive()

            // Diagnostics
            .AddSingleton<UEAssetConflictDiagnosticEmitter>()

            // Installers
            .AddSingleton<SmartUEInstaller>();

        return services;
    }
}
