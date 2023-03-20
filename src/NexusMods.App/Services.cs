using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.CLI.Renderers;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.DarkestDungeon;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.Reshade;
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

    public static IServiceCollection AddApp(this IServiceCollection services, bool addStandardGameLocators = true)
    {
        // TODO: Add File Extractor Here
        services.AddCLI()
            .AddFileSystem()
            .AddUI()
            .AddFileExtractors()
            .AddDataModel()
            .AddBethesdaGameStudios()
            .AddRedEngineGames()
            .AddGenericGameSupport()
            .AddReshade()
            .AddDarkestDungeon()
            .AddRenderers()
            .AddNexusWebApi()
            .AddAdvancedHttpDownloader()
            .AddTestHarness()
            .AddSingleton<HttpClient>()
            .AddCommon();


        if (addStandardGameLocators)
            services.AddStandardGameLocators();

        return services;
    }
}
