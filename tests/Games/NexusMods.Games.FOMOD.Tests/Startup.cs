using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.BuildInfo;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.FOMOD.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddRedEngineGames()
            .AddLoadoutAbstractions()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.6.659.0"))
            .AddFomod()
            .AddSingleton<ICoreDelegates, MockDelegates>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddGames()
            .Validate();
    }
}
