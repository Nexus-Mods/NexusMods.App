using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Settings;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.FileExtractor;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Jobs;
using NexusMods.Library;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection container)
    {
        ConfigureTestedServices(container);
        container.AddLogging(builder => builder.AddXunitOutput());
    }
    
    public static void ConfigureTestedServices(IServiceCollection container)
    {
        AddServices(container);
    }
    
    public static IServiceCollection AddServices(IServiceCollection container)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.DataModel.Tests-{Guid.NewGuid()}";

        var prefix = FileSystem.Shared
            .GetKnownPath(baseKnownPath)
            .Combine(baseDirectory);

        return container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.61"))
            .AddRedEngineGames()
            .AddLoadoutAbstractions()
            .Validate();
    }
}

