using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.HttpDownloader;

internal static class DownloaderVerbs
{
    internal static IServiceCollection AddDownloaderVerbs(this IServiceCollection services) =>
        services.AddVerb(() => DownloadUrl);

    [Verb("download-url", "Download a file from a given URL")]
    private static async Task<int> DownloadUrl([Injected] IRenderer renderer,
        [Option("u", "url", "The url of the file to download")] Uri uri,
        [Option("o", "output", "The path to save the file to")] AbsolutePath output,
        [Injected] IHttpDownloader httpDownloader,
        [Injected] CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        var hash = await renderer.WithProgress(token, async () =>
        {
            return await httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                output, null, null, token);
        });

        var elapsed = sw.Elapsed;
        await renderer.Table(new[] { "File", "Hash", "Size", "Elapsed", "Speed" },
            new[] { new object[] { output, hash, output.FileInfo.Size, elapsed, output.FileInfo.Size / elapsed } });
        return 0;
    }

}
