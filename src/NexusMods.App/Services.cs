using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.Commandline;
using NexusMods.App.UI;
using NexusMods.App.UI.Settings;
using NexusMods.Backend;
using NexusMods.Backend.Games.Locators;
using NexusMods.CLI;
using NexusMods.Collections;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Games.FileHashes;
using NexusMods.Games.FOMOD;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Games.Generic;
using NexusMods.Library;
using NexusMods.Networking.EpicGameStore;
using NexusMods.Networking.GitHub;
using NexusMods.Networking.GOG;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.Steam;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.ProxyConsole;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using NexusMods.SingleProcess;
using NexusMods.Telemetry;

namespace NexusMods.App;

public static class Services
{
    public static IServiceCollection AddApp(
        this IServiceCollection services,
        TrackingSettings? trackingSettings = null,
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
                .AddEpicGameStore()
                .AddSingleton<TimeProvider>(_ => TimeProvider.System)
                .AddDataModel()
                .AddLibrary()
                .AddLibraryModels()
                .AddJobMonitor()
                .AddNexusModsCollections()

                .AddSettings<TrackingSettings>()
                .AddSettings<LoggingSettings>()
                .AddSettings<ExperimentalSettings>()
                .AddDefaultRenderers()
                .AddDefaultParsers()

                .AddSingleton<ITelemetryProvider, TelemetryProvider>()
                .AddTelemetry(trackingSettings)
                .AddTrackingOld(trackingSettings)
                .AddTracking(trackingSettings)

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
                .AddSupportedGames()
                .AddOSInterop()
                .AddRuntimeDependencies()
                .AddGames()
                .AddGameServices()
                .AddGenericGameSupport()
                .AddLoadoutAbstractions()
                .AddFomod()
                .AddNexusWebApi()
                .AddHttpDownloader()
                // .AddAdvancedHttpDownloader()
                .AddFileSystem()
                .AddCleanupVerbs()
                .AddStatusVerbs()
                .AddSteamCli()
                .AddGOG()
                .AddFileHashes()
                .AddGitHubApi();

            if (!startupMode.IsAvaloniaDesigner)
                services.AddSingleProcess(Mode.Main);

            if (addStandardGameLocators)
                services.AddGameLocators(settings: gameLocatorSettings);
        }
        else
        {
            services
                .AddSingleton<TimeProvider>(_ => TimeProvider.System)
                .AddFileSystem()
                .AddOSInterop()
                .AddRuntimeDependencies()
                .AddDefaultRenderers()
                .AddSettingsManager()
                .AddSingleton<JsonConverter, AbsolutePathConverter>()
                .AddSerializationAbstractions()
                .AddSettings<LoggingSettings>();

            if (!startupMode.IsAvaloniaDesigner)
                services.AddSingleProcess(Mode.Client);
        }

        return services;
    }
    
    private static IServiceCollection AddSupportedGames(this IServiceCollection services)
    {
        Games.RedEngine.Services.AddRedEngineGames(services);
        Games.StardewValley.Services.AddStardewValley(services);
        Games.Larian.BaldursGate3.Services.AddBaldursGate3(services);
        Games.CreationEngine.Services.AddCreationEngine(services);
        Games.MountAndBlade2Bannerlord.Services.AddMountAndBlade2Bannerlord(services);
        return services;
    }
}
