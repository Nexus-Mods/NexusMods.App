using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.RedEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators();
        container.AddRedEngineGames();
        container.AddNexusWebApi();
        container.AddHttpDownloader();

        container.AddSingleton<HttpClient>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        container.AddDataModel(KnownFolders.EntryFolder.Join("DataModel", Guid.NewGuid().ToString()));
        container.AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(s =>
            new Resource<FileContentsCache, Size>("File Analysis"));
        container.AddAllSingleton<IResource, IResource<IExtractor, Size>>(s =>
            new Resource<IExtractor, Size>("File Extraction"));
        container.AddFileExtractors();

        container.AddSingleton<TemporaryFileManager>(s => 
            new TemporaryFileManager(KnownFolders.EntryFolder.Join("tempFiles")));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

