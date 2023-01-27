using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators(false);
        container.AddStubbedGameLocators();
        container.AddCLI();
        container.AddAllScoped<IRenderer, LoggingRenderer>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

