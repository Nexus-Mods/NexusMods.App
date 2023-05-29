using NexusMods.CLI.Types;
using NexusMods.DataModel;
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
    private readonly IEnumerable<IDownloadProtocolHandler> _handlers;
    private readonly IRenderer _renderer;
    private readonly ArchiveAnalyzer _archiveAnalyzer;
    private readonly ArchiveInstaller _archiveInstaller;

    /// <summary/>
    public DownloadAndInstallMod(IHttpDownloader httpDownloader, Configurator configurator, TemporaryFileManager temp,
        IEnumerable<IDownloadProtocolHandler> handlers, ArchiveInstaller archiveInstaller, ArchiveAnalyzer archiveAnalyzer)
    {
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;
        _httpDownloader = httpDownloader;
        _temp = temp;
        _handlers = handlers;
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
        await using var tempDir = _temp.CreateFile();
        await _renderer.WithProgress(token, async () =>
        {
            var uri = new Uri(url);
            var handler = _handlers.FirstOrDefault(x => x.Protocol == uri.Scheme);
            if (handler != null)
            {
                await handler.Handle(url, loadout, modName, default);
                return 0;
            }

            await _httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                tempDir.Path, null, token);
            
            var hash = await _archiveAnalyzer.AnalyzeFileAsync(tempDir, token);
            await _archiveInstaller.AddMods(loadout.Value.LoadoutId, hash.Hash, token:token);
            return 0;
        });

        return 0;
    }
}
