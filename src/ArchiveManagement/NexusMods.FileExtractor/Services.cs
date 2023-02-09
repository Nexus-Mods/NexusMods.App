using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;

namespace NexusMods.FileExtractor;

/// <summary>
/// Functionality related to Dependency Injection.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds file extraction related services to the provided DI container.
    /// </summary>
    /// <param name="coll">Service collection to register.</param>
    /// <returns>Service collection passed as parameter.</returns>
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll)
    {
        coll.AddSingleton<FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager>();
        coll.TryAddSingleton<IResource<IExtractor, Size>>(s => new Resource<IExtractor, Size>("File Extraction"));
        return coll;
    }
}