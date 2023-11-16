using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.App.CLI.Renderers;
using NexusMods.App.Listeners;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.GlobalSettings;
using NexusMods.FileExtractor;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.DarkestDungeon;
using NexusMods.Games.FOMOD;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Games.Generic;
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
        AppConfig? config = null, bool addStandardGameLocators = true)
    {
        config ??= new AppConfig();

        services
            .AddSingleton<IAppConfigManager, AppConfigManager>(provider => new AppConfigManager(config, provider.GetRequiredService<JsonSerializerOptions>()))
            .AddCLI()
            .AddFileSystem()
            .AddUI(config.LauncherSettings)
            .AddGuidedInstallerUi()
            .AddAdvancedInstallerUi()
            .AddFileExtractors(config.FileExtractorSettings)
            .AddDataModel(config.DataModelSettings)
            .AddBethesdaGameStudios()
            .AddRedEngineGames()
            .AddGenericGameSupport()
            .AddReshade()
            .AddFomod()
            .AddDarkestDungeon()
            .AddSifu()
            .AddStardewValley()
            .AddNexusWebApi()
            .AddNexusWebApiNmaIntegration()
            .AddAdvancedHttpDownloader(config.HttpDownloaderSettings)
            .AddTestHarness()
            .AddSingleton<HttpClient>()
            .AddListeners()
            .AddCommon()
            .AddDownloaders();

        if (addStandardGameLocators)
            services.AddStandardGameLocators();

        return OpenTelemetryRegistration.AddTelemetry(services, new OpenTelemetrySettings
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
    }
}
