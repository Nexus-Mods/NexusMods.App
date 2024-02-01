using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusMods.Abstractions.FileExtractor;
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
    /// <param name="settings">Settings for the extractor.</param>
    /// <returns>Service collection passed as parameter.</returns>
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll, IFileExtractorSettings? settings = null)
    {
        if (settings == null)
            coll.AddSingleton<IFileExtractorSettings, FileExtractorSettings>();
        else
            coll.AddSingleton(settings);

        coll.AddFileExtractorVerbs();
        coll.AddSingleton<IFileExtractor, FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager, TemporaryFileManagerEx>();
        return coll;
    }
}
