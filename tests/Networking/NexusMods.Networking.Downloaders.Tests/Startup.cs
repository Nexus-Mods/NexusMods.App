using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Settings;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Settings;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Networking.Downloaders.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
            .AddUniversalGameLocator<SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddStubbedGameLocators()
            .AddBethesdaGameStudios()
            .AddGenericGameSupport()
            .AddRedEngineGames()
            .AddSerializationAbstractions()
            .AddFomod()
            .AddDownloaders()
            .AddSingleton<LocalHttpServer>()
            .Validate();
    }
}

