using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Activities;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Games.FOMOD;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Games.Generic;
using NexusMods.Games.Reshade;
using NexusMods.Games.TestHarness;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.Settings;
using NexusMods.SingleProcess;
using NexusMods.StandardGameLocators;
using NexusMods.Telemetry;

#if NEXUSMODS_APP_ENABLE_BETHESDA
using NexusMods.Games.BethesdaGameStudios;
#endif
#if NEXUSMODS_APP_ENABLE_BLADE_AND_SORCERY
using NexusMods.Games.BladeAndSorcery;
#endif
#if NEXUSMODS_APP_ENABLE_DARKEST_DUNGEON
using NexusMods.Games.DarkestDungeon;
#endif
#if NEXUSMODS_APP_ENABLE_BANNERLORD
using NexusMods.Games.MountAndBlade2Bannerlord;
#endif
#if NEXUSMODS_APP_ENABLE_CYBERPUNK_2077
using NexusMods.Games.RedEngine;
#endif
#if NEXUSMODS_APP_ENABLE_SIFU
using NexusMods.Games.Sifu;
#endif
#if NEXUSMODS_APP_ENABLE_STARDEW_VALLEY
using NexusMods.Games.StardewValley;
#endif

namespace NexusMods.App;

public static class Services
{

    public static IServiceCollection AddApp(this IServiceCollection services,
        TelemetrySettings? telemetrySettings = null,
        bool addStandardGameLocators = true,
        StartupMode? startupMode = null)
    {
        startupMode ??= new StartupMode();
        if (startupMode.RunAsMain)
        {
            services
                .AddSettings<TelemetrySettings>()
                .AddSettings<LoggingSettings>()
                .AddSingleProcess(Mode.Main)
                .AddDefaultRenderers()

                .AddSingleton<ITelemetryProvider, TelemetryProvider>()
                .AddTelemetry(telemetrySettings ?? new TelemetrySettings())

                .AddSingleton<CommandLineConfigurator>()
                .AddCLI()
                .AddUI()
                .AddSettingsManager()
                .AddSingleton<App>()
                .AddGuidedInstallerUi()
                .AddAdvancedInstaller()
                .AddAdvancedInstallerUi()
                .AddFileExtractors()
                .AddDataModel()
                .AddSerializationAbstractions()
                .AddInstallerTypes()
                .AddSupportedGames()
                .AddActivityMonitor()
                .AddCrossPlatform()
                .AddGames()
                .AddGenericGameSupport()
                .AddFileStoreAbstractions()
                .AddLoadoutAbstractions()
                .AddReshade()
                .AddFomod()
                .AddNexusWebApi()
                .AddAdvancedHttpDownloader()
                .AddTestHarness()
                .AddSingleton<HttpClient>()
                .AddFileSystem()
                .AddDownloaders();

            if (addStandardGameLocators)
                services.AddStandardGameLocators();
        }
        else
        {
            services.AddFileSystem()
                .AddCrossPlatform()
                .AddSingleProcess(Mode.Client)
                .AddDefaultRenderers()
                .AddSettingsManager();
        }

        return services;
    }
    
    private static IServiceCollection AddSupportedGames(this IServiceCollection services)
    {
#if NEXUSMODS_APP_ENABLE_BETHESDA
        services.AddBethesdaGameStudios();
#endif
#if NEXUSMODS_APP_ENABLE_CYBERPUNK_2077
        services.AddRedEngineGames();
#endif
#if NEXUSMODS_APP_ENABLE_DARKEST_DUNGEON
        services.AddDarkestDungeon();
#endif
#if NEXUSMODS_APP_ENABLE_BLADE_AND_SORCERY
        services.AddBladeAndSorcery();
#endif
#if NEXUSMODS_APP_ENABLE_SIFU
        services.AddSifu();
#endif
#if NEXUSMODS_APP_ENABLE_STARDEW_VALLEY
        services.AddStardewValley();
#endif
#if NEXUSMODS_APP_ENABLE_BANNERLORD
        services.AddMountAndBladeBannerlord();
#endif

        return services;
    }
}
