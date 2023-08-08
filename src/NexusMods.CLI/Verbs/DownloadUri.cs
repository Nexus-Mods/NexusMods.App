using System.Diagnostics;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

/// <summary>
/// Downloads a file from a given URI, outputting it to a specified location.
/// </summary>
public class DownloadUri : AVerb<Uri, AbsolutePath>, IRenderingVerb
{
    private readonly IHttpDownloader _httpDownloader;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// The URI to download files from.
    /// </summary>
    /// <param name="httpDownloader">Allows for downloads of content from given URLs.</param>
    /// <param name="configurator">Used for late binding of renderers.</param>
    /// <remarks>Usually called from DI container.</remarks>
    public DownloadUri(IHttpDownloader httpDownloader)
    {
        _httpDownloader = httpDownloader;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("download-uri",
        "Downloads a file from a given URI",
        new OptionDefinition[]
        {
            new OptionDefinition<Uri>("u", "uri", "URI to download"),
            new OptionDefinition<AbsolutePath>("o", "output", "Output file"),
        });


    /// <inheritdoc />
    public async Task<int> Run(Uri uri, AbsolutePath output, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        var hash = await Renderer.WithProgress(token, async () =>
        {
            return await _httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                output, null, null, token);
        });

        var elapsed = sw.Elapsed;
        await Renderer.Render(new Table(new[] { "File", "Hash", "Size", "Elapsed", "Speed" },
            new[] { new object[] { output, hash, output.FileInfo.Size, elapsed, output.FileInfo.Size / elapsed } }));
        return 0;
    }
}
