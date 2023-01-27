using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.StandardGameLocators.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators(false);
        container.AddSingleton<TemporaryFileManager>();
        container.AddStubbedGameLocators();
        container.AddAllSingleton<IModInstaller, StubbedGameInstaller>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}
