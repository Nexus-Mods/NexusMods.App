using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.RedEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
            .AddRedEngineGames()
            .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

