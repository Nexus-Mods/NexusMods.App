using Microsoft.Extensions.DependencyInjection;
using NexusMods.FileExtractor.Extractors;

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
        return coll;
    }
}