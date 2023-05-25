using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.App.Listeners;

/// <summary>
/// Listens to incoming RPC requests to download files.
/// </summary>
public class NxmRpcListener : IDisposable
{
    private readonly Client _client;
    private readonly ILogger<NxmRpcListener> _logger;
    private BufferBlock<NXMUrlMessage> _urlsBlock;
    private CancellationTokenSource _cancellationSource;
    private IHttpDownloader _downloader;
    private readonly TemporaryFileManager _temp;

    // Note: Temporary until we implement the UI part.
    private LoadoutManager _loadoutManager;

    public NxmRpcListener(IMessageConsumer<NXMUrlMessage> nxmUrlMessages, ILogger<NxmRpcListener> logger, Client client, IHttpDownloader downloader, TemporaryFileManager temp, LoadoutManager loadoutManager)
    {
        _logger = logger;
        _client = client;
        _downloader = downloader;
        _temp = temp;
        _loadoutManager = loadoutManager;
        _cancellationSource = new CancellationTokenSource();
        _urlsBlock = new BufferBlock<NXMUrlMessage>();
        Task.Run(ListenAsync);
        nxmUrlMessages.Messages
            .Where(url => url.Value.UrlType == NXMUrlType.Mod)
            .Subscribe(item => _urlsBlock.Post(item));
    }

    private async Task ListenAsync()
    {
        // Note: Exceptions logged in `RxApp.DefaultExceptionHandler`
        while (await _urlsBlock.OutputAvailableAsync(_cancellationSource.Token))
        {
            var item = await _urlsBlock.ReceiveAsync(_cancellationSource.Token);
            
            // The following code is wrapped in an exception handler such that on possible unhandled exception,
            // the service keeps running rather than terminated.
            try
            {
                await ProcessUrlAsync(item.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception when downloading a mod.");
            }
        }
    }

    private async Task ProcessUrlAsync(NXMUrl parsed)
    {
        // TODO Note: This code is temporary, we need a UI design for this; where user selects a loadout for a game.
        // Then we would wrap this task in a class, and pass it off to the UI ViewModel (e.g. via event), from where
        // the user can cancel/pause/resume the task etc.
        // Then we pass this URL somewhere else (e.g. via an event) to a ViewModel which will be seen by the UI.
        
        using var tempPath = _temp.CreateFile();
        var loadout = _loadoutManager.Registry.AllLoadouts().First(x => x.Installation.Game.Domain == parsed.Mod.Game);
        var marker = new LoadoutMarker(_loadoutManager, loadout.LoadoutId);
        
        Response<DownloadLink[]> links;
        if (parsed.Key == null)
            links = await _client.DownloadLinksAsync(parsed.Mod.Game, parsed.Mod.ModId, parsed.Mod.FileId);
        else
            links = await _client.DownloadLinksAsync(parsed.Mod.Game, parsed.Mod.ModId, parsed.Mod.FileId, parsed.Key.Value, parsed.ExpireTime!.Value);
        
        var downloadUris = links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
        await _downloader.DownloadAsync(downloadUris, tempPath);

        var file = (await _client.ModFilesAsync(parsed.Mod.Game, parsed.Mod.ModId)).Data.Files.First(x => x.FileId == parsed.Mod.FileId);
        await marker.InstallModsFromArchiveAsync(tempPath, file.Name);
    }

    /// <summary>
    /// Stops listening for background events.
    /// </summary>
    public void Dispose()
    {
        _cancellationSource.Cancel();
        _urlsBlock.Complete();
        _cancellationSource.Dispose();
    }
}
