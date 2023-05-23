using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

/// <summary>
/// Downloads a mod with NXM protocol.
/// </summary>
public class DownloadAndInstallMod : AVerb<string, LoadoutMarker, string>
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temp;
    private readonly IRenderer _renderer;
    
    /// <summary/>
    public DownloadAndInstallMod(IHttpDownloader httpDownloader, Configurator configurator, TemporaryFileManager temp)
    {
        _httpDownloader = httpDownloader;
        _temp = temp;
        _renderer = configurator.Renderer;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("download-and-install-mod",
        "Downloads a mod (via NXM protocol, or direct link to file) and installs it in one step.",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("u", "url", "The URL to handle (nxm:// or direct link to file)"),
            new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to add the mod to"),
            new OptionDefinition<string>("n", "name", "Name of the mod after installing")
        });

    /// <inheritdoc />
    public async Task<int> Run(string url, LoadoutMarker loadout, string modName, CancellationToken token)
    {
        using var tempDir = _temp.CreateFile();
        await _renderer.WithProgress(token, async () =>
        {
            var uri = new Uri(url);
            await _httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                tempDir.Path, null, token);
            
            return await loadout.InstallModsFromArchiveAsync(tempDir.Path, modName, token);
        });

        return 0;
    }
}
