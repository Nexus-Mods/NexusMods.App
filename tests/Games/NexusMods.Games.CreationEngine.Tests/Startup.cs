using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.FOMOD;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.CreationEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<CreationEngine.SkyrimSE.SkyrimSE>(new Version("1.6.1170"))
            .AddFomod()
            .AddCreationEngine()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddLoadoutAbstractions()
            .Validate();
    }
}

