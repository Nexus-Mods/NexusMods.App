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
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.BladeAndSorcery;
using NexusMods.Games.DarkestDungeon;
using NexusMods.Games.FOMOD;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Games.Generic;
using NexusMods.Games.MountAndBlade2Bannerlord;
using NexusMods.Games.RedEngine;
using NexusMods.Games.Reshade;
using NexusMods.Games.Sifu;
using NexusMods.Games.StardewValley;
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
                .AddGames()
                .AddActivityMonitor()
                .AddCrossPlatform()
                .AddBethesdaGameStudios()
                .AddRedEngineGames()
                .AddGenericGameSupport()
                .AddFileStoreAbstractions()
                .AddLoadoutAbstractions()
                .AddReshade()
                .AddFomod()
                .AddDarkestDungeon()
                .AddBladeAndSorcery()
                .AddSifu()
                .AddStardewValley()
                .AddMountAndBladeBannerlord()
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
}
