using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.RedEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
            .AddRedEngineGames()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddSerializationAbstractions()
            .AddLoadoutAbstractions()
            .AddFileStoreAbstractions()
            .AddInstallerTypes()
            .Validate();
    }
}

