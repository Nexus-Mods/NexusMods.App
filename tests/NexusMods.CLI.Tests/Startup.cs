using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.Tests;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators(false);
        container.AddSingleton<IGame, StubbedGame>();
        container.AddSingleton<IModInstaller, StubbedGameInstaller>();

        container.AddSingleton<AHandler<SteamGame, int>, StubbedSteamLocator>();
        container.AddSingleton<AHandler<GOGGame, long>, StubbedGogLocator>();        
        container.AddCLI();
        container.AddCLIVerbs();
        container.AddAllScoped<IRenderer, LoggingRenderer>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

