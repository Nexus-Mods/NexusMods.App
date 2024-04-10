using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.Listeners;
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
using NexusMods.Telemetry.OpenTelemetry;
using OpenTelemetry.Exporter;

namespace NexusMods.App;

public static class Services
{
    public static IServiceCollection AddListeners(this IServiceCollection services)
    {
        services.AddSingleton<NxmRpcListener>();
        return services;
    }

    public static IServiceCollection AddApp(this IServiceCollection services,
        AppConfig? config = null,
        bool addStandardGameLocators = true,
        bool slimMode = false)
    {
        config ??= new AppConfig();

        if (!slimMode)
        {
            services
                .AddSingleton<CommandLineConfigurator>()
                .AddCLI()
                .AddUI()
                .AddSettings()
                .AddSingleton<App>()
                .AddGuidedInstallerUi()
                .AddAdvancedInstaller()
                .AddAdvancedInstallerUi()
                .AddFileExtractors()
                .AddDataModel(config.DataModelSettings)
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
                .AddListeners()
                .AddDownloaders();

            services = OpenTelemetryRegistration.AddTelemetry(services, new OpenTelemetrySettings
            {
                // TODO: pull from settings
                // IsEnabled = config.EnableTelemetry ?? false,
                IsEnabled = false,

                EnableMetrics = true,
                EnableTracing = true,

                ApplicationName = Telemetry.LibraryInfo.AssemblyName,
                ApplicationVersion = Telemetry.LibraryInfo.AssemblyVersion,

                ExporterProtocol = OtlpExportProtocol.HttpProtobuf,
                ExporterMetricsEndpoint = new Uri("https://collector.nexusmods.com/v1/metrics"),
                ExporterTracesEndpoint = new Uri("https://collector.nexusmods.com/v1/traces"),
            }).ConfigureTelemetry(Telemetry.LibraryInfo, configureMetrics: Telemetry.SetupTelemetry);


            if (addStandardGameLocators)
                services.AddStandardGameLocators();

        }

        services
            .AddFileSystem()
            .AddSingleton<IStartupHandler, StartupHandler>()
            .AddSingleProcess()
            .AddSingleton(s =>
            {
                var fs = s.GetRequiredService<IFileSystem>();
                var directory = fs.OS.MatchPlatform(
                    () => fs.GetKnownPath(KnownPath.TempDirectory),
                    () => fs.GetKnownPath(KnownPath.XDG_RUNTIME_DIR),
                    () => fs.GetKnownPath(KnownPath.ApplicationDataDirectory).Combine("NexusMods_App")
                );

                return new SingleProcessSettings
                {
                    SyncFile = directory.Combine("NexusMods.App-single_process.sync")
                };
            })
            .AddDefaultRenderers();

        return services;
    }
}
