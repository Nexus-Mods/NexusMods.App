using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Avowed;
using NexusMods.Games.UnrealEngine.Emitters;
using NexusMods.Games.UnrealEngine.Installers;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Games.UnrealEngine.Stalker2;
using NexusMods.Games.UnrealEngine.HogwartsLegacy;
using NexusMods.Games.UnrealEngine.PacificDrive;

namespace NexusMods.Games.UnrealEngine;

public static class Services
{
    private static IServiceCollection AddAvowed(this IServiceCollection services)
    {
        services
            .AddGame<AvowedGame>()
            .AddSingleton<ITool, RunGameTool<AvowedGame>>()
            .AddSettings<AvowedSettings>();

        return services;
    }

    private static IServiceCollection AddHogwartsLegacy(this IServiceCollection services)
    {
        services
            .AddGame<HogwartsLegacyGame>()
            .AddSingleton<ITool, RunGameTool<HogwartsLegacyGame>>()
            .AddSettings<HogwartsLegacySettings>();
    
        return services;
    }
    
    private static IServiceCollection AddPacificDrive(this IServiceCollection services)
    {
        services
            .AddGame<PacificDriveGame>()
            .AddSingleton<ITool, RunGameTool<PacificDriveGame>>()
            .AddSettings<PacificDriveSettings>();
    
        return services;
    }
    
    private static IServiceCollection AddStalker2(this IServiceCollection services)
    {
        services
            .AddGame<Stalker2Game>()
            .AddSingleton<ITool, RunGameTool<Stalker2Game>>()
            .AddSettings<Stalker2Settings>();
    
        return services;
    }

    public static IServiceCollection AddUnrealEngineGames(this IServiceCollection services)
    {
        services
            // Misc
            //.AddSingleton<UESynchronizer>()

            // Diagnostics
            .AddSingleton<AssetConflictDiagnosticEmitter>()
            .AddSingleton<ModOverwritesGameFilesEmitter>()
            .AddSingleton<MissingScriptingSystemEmitter>()
            .AddSingleton<MissingMemberVarLayout>()

            // Installers
            .AddSingleton<ScriptingSystemInstaller>()
            .AddSingleton<ScriptingSystemLuaInstaller>()
            .AddSingleton<UnrealEnginePakModInstaller>()

            // Loadout Item Models
            .AddScriptingSystemLoadoutItemGroupModel()
            .AddScriptingSystemLuaLoadoutItemModel()
            .AddUnrealEngineLoadoutItemModel()
            .AddUnrealEngineLogicLoadoutItemModel()
            .AddUnrealEnginePakLoadoutFileModel()

            // Games
            .AddAvowed()
            .AddHogwartsLegacy()
            .AddPacificDrive()
            .AddStalker2()
            
            // Pipelines
            .AddPipelines();

        return services;
    }
}
