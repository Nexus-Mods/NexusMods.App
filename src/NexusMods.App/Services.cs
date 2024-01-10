using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.Listeners;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.GlobalSettings;
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
using NexusMods.Networking.NexusWebApi.NMA;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
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
                .AddSingleton<IAppConfigManager, AppConfigManager>(provider =>
                    new AppConfigManager(config, provider.GetRequiredService<JsonSerializerOptions>()))
                .AddSingleton<CommandLineConfigurator>()
                .AddCLI()
                .AddUI(config.LauncherSettings)
                .AddSingleton<App>()
                .AddGuidedInstallerUi()
                .AddAdvancedInstaller()
                .AddAdvancedInstallerUi()
                .AddFileExtractors(config.FileExtractorSettings)
                .AddDataModel(config.DataModelSettings)
                .AddBethesdaGameStudios()
                .AddRedEngineGames()
                .AddGenericGameSupport()
                .AddReshade()
                .AddFomod()
                .AddDarkestDungeon()
                .AddBladeAndSorcery()
                .AddSifu()
                .AddStardewValley()
                .AddMountAndBladeBannerlord()
                .AddNexusWebApi()
                .AddNexusWebApiNmaIntegration()
                .AddAdvancedHttpDownloader(config.HttpDownloaderSettings)
                .AddTestHarness()
                .AddSingleton<HttpClient>()
                .AddListeners()
                .AddCommon()
                .AddDownloaders();

            services = OpenTelemetryRegistration.AddTelemetry(services, new OpenTelemetrySettings
            {
                IsEnabled = config.EnableTelemetry ?? false,

                EnableMetrics = true,
                EnableTracing = true,

                ApplicationName = Telemetry.LibraryInfo.AssemblyName,
                ApplicationVersion = Telemetry.LibraryInfo.AssemblyVersion,

                ExporterProtocol = OtlpExportProtocol.HttpProtobuf,
                ExporterMetricsEndpoint = new Uri("https://collector.nexusmods.com/v1/metrics"),
                ExporterTracesEndpoint = new Uri("https://collector.nexusmods.com/v1/traces")
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
                    () => throw new PlatformNotSupportedException()
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
