using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.DataModel.CommandLine.Verbs;

public static class FileHashCacheVerbs
{
    public static IServiceCollection AddFileHashCacheVerbs(this IServiceCollection services) =>
        services
            .AddVerb(() => HashFolder);

    [Verb("hash-folder", "Hashes all files in a folder and stores them in a cache")]
    private static async Task<int> HashFolder([Injected] IRenderer renderer,
        [Option("i", "inputFolder", "Input folder to hash")] AbsolutePath inputFolder,
        [Injected] FileHashCache fileHashCache,
        [Injected] CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        var results = await renderer.WithProgress(token, async () =>
            await fileHashCache.IndexFolderAsync(inputFolder, token).ToArrayAsync(cancellationToken: token));

        await renderer.Table(new[] { "Path", "Hash", "Size"},
            results.OrderBy(entry => entry.Path.RelativeTo(inputFolder))
                .Select(entry => new object[] {entry.Path.RelativeTo(inputFolder), entry.Hash, entry.Size}));
        return 0;
    }

}
