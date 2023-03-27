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
using NexusMods.Paths.Utilities;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.RedEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = KnownFolders.EntryFolder.CombineUnchecked("DataModel").CombineUnchecked(Guid.NewGuid().ToString());
        container.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
                 .AddRedEngineGames()
                 .AddNexusWebApi(true)
                 .AddHttpDownloader()

                 .AddSingleton<HttpClient>()
                 .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
                 .AddDataModel(new DataModelSettings(prefix))
                 .AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(_ =>
            new Resource<FileContentsCache, Size>("File Analysis"))
                 .AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ =>
            new Resource<IExtractor, Size>("File Extraction"))
                 .AddFileExtractors(new FileExtractorSettings())

                 .AddSingleton<TemporaryFileManager>(_ =>
            new TemporaryFileManager(KnownFolders.EntryFolder.CombineUnchecked("tempFiles")))
                 .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

