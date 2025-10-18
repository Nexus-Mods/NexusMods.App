using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Backend;
using NexusMods.Games.Larian.BaldursGate3;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.Larian.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Larian.BaldursGate3.BaldursGate3>(new Version("1.61"))
            .AddBaldursGate3()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddGameServices()
            .AddLoadoutAbstractions()
            .Validate();
    }
}
