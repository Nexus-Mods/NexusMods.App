using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Games.RedEngine;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.UI.Theme.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"));
        container.AddRedEngineGames();
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

