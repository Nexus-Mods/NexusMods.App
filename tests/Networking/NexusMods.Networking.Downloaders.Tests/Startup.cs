using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Networking.Downloaders.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Dummy: We're not injecting anything yet; this is for a time we will need to.
        services.AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
            .AddUniversalGameLocator<SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddStubbedGameLocators()
            .AddBethesdaGameStudios()
            .AddGenericGameSupport()
            .AddRedEngineGames()
            .AddFomod()
            .AddDownloaders()
            .AddAllSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<LocalHttpServer>()
            .AddAllSingleton<IGuidedInstaller, CliGuidedInstaller>()
            .Validate();
    }
}

