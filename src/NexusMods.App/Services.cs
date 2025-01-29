using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.Commandline;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Collections;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Games.FileHashes;
using NexusMods.Games.FOMOD;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Games.Generic;
using NexusMods.Games.TestHarness;
using NexusMods.Jobs;
using NexusMods.Library;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.GOG;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.Steam;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
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
        services.Configure<HostOptions>(options =>
        {
            // Sequential execution can lead to long startup times depending on number of hostedServices.
            options.ServicesStartConcurrently = true;
            // If executed sequentially, one service taking a long time can trigger the timeout,
            // preventing StopAsync of other services from being called. 
            options.ServicesStopConcurrently = true;
        });
        startupMode ??= new StartupMode();
        if (startupMode.RunAsMain)
        {
            services
                .AddDataModel()
                .AddLibrary()
                .AddLibraryModels()
                .AddJobMonitor()
                .AddNexusModsCollections()

                .AddSettings<TelemetrySettings>()
                .AddSettings<LoggingSettings>()
                .AddSettings<ExperimentalSettings>()
                .AddDefaultRenderers()
                .AddDefaultParsers()

                .AddSingleton<ITelemetryProvider, TelemetryProvider>()
                .AddTelemetry(telemetrySettings ?? new TelemetrySettings())
                .AddTracking(telemetrySettings ?? new TelemetrySettings())

                .AddSingleton<CommandLineConfigurator>()
                .AddCLI()
                .AddUI()
                .AddSettingsManager()
                .AddSingleton<App>()
                .AddGuidedInstallerUi()
                .AddAdvancedInstaller()
                .AddAdvancedInstallerUi()
                .AddFileExtractors()
                .AddSerializationAbstractions()
                .AddSupportedGames(experimentalSettings)
                .AddCrossPlatform()
                .AddGames()
                .AddGenericGameSupport()
                .AddLoadoutAbstractions()
                .AddFomod()
                .AddNexusWebApi()
                .AddHttpDownloader()
                // .AddAdvancedHttpDownloader()
                .AddTestHarness()
                .AddFileSystem()
                .AddDownloaders()
                .AddCleanupVerbs()
                .AddSteamCli()
                .AddGOG()
                .AddFileHashes();

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
        }
        
        Games.RedEngine.Services.AddRedEngineGames(services);
        Games.StardewValley.Services.AddStardewValley(services);
        Games.Larian.BaldursGate3.Services.AddBaldursGate3(services);
        Games.MountAndBlade2Bannerlord.Services.AddMountAndBlade2Bannerlord(services);
        return services;
    }
}
