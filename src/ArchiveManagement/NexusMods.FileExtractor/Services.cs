using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Settings;
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
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll)
    {
        coll.AddSettings<FileExtractorSettings>();
        coll.AddFileExtractorVerbs();
        coll.AddSingleton<IFileExtractor, FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager, TemporaryFileManagerEx>();
        return coll;
    }
}
