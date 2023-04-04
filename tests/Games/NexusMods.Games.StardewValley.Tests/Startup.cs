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
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.StardewValley.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<HttpClient>()
            .AddNexusWebApi(true)
            .AddHttpDownloader()
            .AddDataModel(new DataModelSettings())
            .AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(_ => new Resource<FileContentsCache, Size>("File Analysis"))
            .AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"))
            .AddFileExtractors()
            .AddUniversalGameLocator<StardewValley>(new Version(1, 0))
            .AddStardewValley()
            .Validate();
    }
}
