using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Activities;
using NexusMods.App.Commandline;
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
                .AddDownloaders()
                .AddCleanupVerbs();

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
        Games.BethesdaGameStudios.Services.AddBethesdaGameStudios(services);
#endif
#if NEXUSMODS_APP_ENABLE_CYBERPUNK_2077
        Games.RedEngine.Services.AddRedEngineGames(services);
#endif
#if NEXUSMODS_APP_ENABLE_DARKEST_DUNGEON
        Games.DarkestDungeon.Services.AddDarkestDungeon(services);
#endif
#if NEXUSMODS_APP_ENABLE_BLADE_AND_SORCERY
        Games.BladeAndSorcery.Services.AddBladeAndSorcery(services);
#endif
#if NEXUSMODS_APP_ENABLE_SIFU
        Games.Sifu.Services.AddSifu(services);
#endif
#if NEXUSMODS_APP_ENABLE_STARDEW_VALLEY
        Games.StardewValley.Services.AddStardewValley(services);
#endif
#if NEXUSMODS_APP_ENABLE_BANNERLORD
        Games.MountAndBlade2Bannerlord.ServicesExtensions.AddMountAndBladeBannerlord(services);
#endif
        return services;
    }
}
