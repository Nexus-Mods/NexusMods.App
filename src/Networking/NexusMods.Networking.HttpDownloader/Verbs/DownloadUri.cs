using System.Diagnostics;
using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Verbs;

/// <summary>
/// Downloads a file from a given URI, outputting it to a specified location.
/// </summary>
public class DownloadUri : AVerb<Uri, AbsolutePath>
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly IRenderer _renderer;

    /// <summary>
    /// The URI to download files from.
    /// </summary>
    /// <param name="httpDownloader">Allows for downloads of content from given URLs.</param>
    /// <param name="configurator">Used for late binding of renderers.</param>
    /// <remarks>Usually called from DI container.</remarks>
    public DownloadUri(IHttpDownloader httpDownloader, Configurator configurator)
    {
        _httpDownloader = httpDownloader;
        _renderer = configurator.Renderer;
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
        var hash = await _renderer.WithProgress(token, async () =>
        {

            return await _httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                output, null, token);

        });

        var elapsed = sw.Elapsed;
        await _renderer.Render(new Table(new[] { "File", "Hash", "Size", "Elapsed", "Speed" },
            new[] { new object[] { output, hash, output.Length, elapsed, output.Length / elapsed } }));
        return 0;
    }
}
