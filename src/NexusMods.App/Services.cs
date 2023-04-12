using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.CLI.Renderers;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.DarkestDungeon;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.Reshade;
using NexusMods.Games.Sifu;
using NexusMods.Games.TestHarness;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;

namespace NexusMods.App;

public static class Services
{
    public static IServiceCollection AddRenderers(this IServiceCollection services)
    {
        services.AddScoped<IRenderer, CLI.Renderers.Spectre>();
        services.AddScoped<IRenderer, Json>();
        return services;
    }

    public static IServiceCollection AddApp(this IServiceCollection services,
        AppConfig? config = null, bool addStandardGameLocators = true)
    {
        config ??= new AppConfig();


        services.AddCLI()
            .AddFileSystem()
            .AddUI()
            .AddFileExtractors(config.FileExtractorSettings)
            .AddDataModel(config.DataModelSettings)
            .AddBethesdaGameStudios()
            .AddRedEngineGames()
            .AddGenericGameSupport()
            .AddReshade()
            .AddFomod()
            .AddDarkestDungeon()
            .AddSifu()
            .AddRenderers()
            .AddNexusWebApi()
            .AddAdvancedHttpDownloader(config.HttpDownloaderSettings)
            .AddTestHarness()
            .AddSingleton<HttpClient>()
            .AddCommon();


        if (addStandardGameLocators)
            services.AddStandardGameLocators();

        return services;
    }
}
