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

namespace NexusMods.Games.TestFramework;

public static class DependencyInjectionHelper
{
    /// <summary>
    /// Adds the following default services to the provided <see cref="IServiceCollection"/> for testing:
    /// <list type="bullet">
    ///     <item>Logging via <see cref="LoggingServiceCollectionExtensions.AddLogging(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/></item>
    ///     <item><see cref="IFileSystem"/> via <see cref="Paths.Services.AddFileSystem"/></item>
    ///     <item><see cref="TemporaryFileManager"/> singleton</item>
    ///     <item><see cref="HttpClient"/> singleton</item>
    ///     <item>Nexus Web API via <see cref="Networking.NexusWebApi.Services.AddNexusWebApi"/></item>
    ///     <item><see cref="IHttpDownloader"/> via <see cref="Networking.HttpDownloader.Services.AddHttpDownloader"/></item>
    ///     <item>All services related to the <see cref="NexusMods.DataModel"/> via <see cref="DataModel.Services.AddDataModel"/></item>
    ///     <item><see cref="IResource{TResource,TUnit}"/> for <see cref="FileContentsCache"/></item>
    ///     <item><see cref="IResource{TResource,TUnit}"/> for <see cref="IExtractor"/></item>
    ///     <item><see cref="IResource{TResource,TUnit}"/> for <see cref="FileHashCache"/></item>
    ///     <item>File extraction services via <see cref="NexusMods.FileExtractor.Services.AddFileExtractors"/></item>
    /// </list>
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultServicesForTesting(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<HttpClient>()
            .AddNexusWebApi(true)
            .AddHttpDownloader()
            .AddDataModel(new DataModelSettings())
            .AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(_ => new Resource<FileContentsCache, Size>("File Analysis for tests"))
            .AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction for tests"))
            .AddAllSingleton<IResource, IResource<FileHashCache, Size>>(_ => new Resource<FileHashCache, Size>("Hash Cache for tests"))
            .AddFileExtractors();
    }
}
