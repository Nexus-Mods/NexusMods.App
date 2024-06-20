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
        StartupMode? startupMode = null,
        ExperimentalSettings? experimentalSettings = null,
        GameLocatorSettings? gameLocatorSettings = null)
    {
        startupMode ??= new StartupMode();
        if (startupMode.RunAsMain)
        {
            services
                .AddSettings<TelemetrySettings>()
                .AddSettings<LoggingSettings>()
                .AddSettings<ExperimentalSettings>()
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
                .AddSupportedGames(experimentalSettings)
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

            if (!startupMode.IsAvaloniaDesigner)
                services.AddSingleProcess(Mode.Main);

            if (addStandardGameLocators)
                services.AddStandardGameLocators(settings: gameLocatorSettings);
        }
        else
        {
            services.AddFileSystem()
                .AddCrossPlatform()
                .AddDefaultRenderers()
                .AddSettingsManager()
                .AddSettings<LoggingSettings>();
            
            if (!startupMode.IsAvaloniaDesigner)
                services.AddSingleProcess(Mode.Client);
        }

        return services;
    }
    
    private static IServiceCollection AddSupportedGames(this IServiceCollection services, ExperimentalSettings? experimentalSettings)
    {
        if (experimentalSettings is { EnableAllGames: true })
        {
            Games.BethesdaGameStudios.Services.AddBethesdaGameStudios(services);
            Games.RedEngine.Services.AddRedEngineGames(services);
            Games.DarkestDungeon.Services.AddDarkestDungeon(services);
            Games.BladeAndSorcery.Services.AddBladeAndSorcery(services);
            Games.Sifu.Services.AddSifu(services);
            Games.MountAndBlade2Bannerlord.ServicesExtensions.AddMountAndBladeBannerlord(services);
        }
        
        Games.StardewValley.Services.AddStardewValley(services);
        return services;
    }
}
