using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using NexusMods.Sdk.FileExtractor;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Backend.FileExtractor;

/// <summary>
/// Extracts files from an archive.
/// </summary>
public static class Verbs
{
    public static IServiceCollection AddFileExtractorVerbs(this IServiceCollection services) =>
        services.AddVerb(() => ExtractArchive);

    [Verb("extract-archive", "Extracts an archive to a folder on-disk")]
    private static async Task<int> ExtractArchive([Option("i", "inputFile", "Input archive to extract")] AbsolutePath inputFile,
        [Option("o", "outputFolder", "Output location for files")] AbsolutePath outputFolder,
        [Injected] IFileExtractor extractor,
        [Injected] CancellationToken token)
    {
        await extractor.ExtractAllAsync(inputFile, outputFolder, token);
        return 0;
    }

}
