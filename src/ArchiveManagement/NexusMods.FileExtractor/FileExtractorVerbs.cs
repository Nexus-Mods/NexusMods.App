using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.FileExtractor;

public static class FileExtractorVerbs
{
    public static IServiceCollection AddFileExtractorVerbs(this IServiceCollection services) =>
        services.AddVerb(() => ExtractArchive);

    [Verb("extract-archive", "Extracts an archive to a folder on-disk")]
    private static async Task<int> ExtractArchive([Option("i", "inputFile", "Input archive to extract")] AbsolutePath inputFile,
        [Option("o", "outputFolder", "Output location for files")] AbsolutePath outputFolder,
        [Injected] FileExtractor extractor,
        CancellationToken token)
    {
        await extractor.ExtractAllAsync(inputFile, outputFolder, token);
        return 0;
    }

}
