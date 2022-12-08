using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.Tests;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddDataModel();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        container.AddStandardGameLocators(false);
        container.AddAllSingleton<IGame, StubbedGame>();
        container.AddSingleton<TemporaryFileManager>();
        container.AddFileExtractors();

        container.AddAllSingleton<AHandler<SteamGame, int>, StubbedSteamLocator>();
        container.AddAllSingleton<AHandler<GOGGame, long>, StubbedGogLocator>();
        container.AddAllSingleton<IModInstaller, StubbedGameInstaller>();

        container.AddAllSingleton<IResource, IResource<ArchiveContentsCache, Size>>(s =>
            new Resource<ArchiveContentsCache, Size>("File Analysis"));
        container.AddAllSingleton<IResource, IResource<IExtractor, Size>>(s =>
            new Resource<IExtractor, Size>("File Extraction"));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

