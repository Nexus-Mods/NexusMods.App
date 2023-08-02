using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.App.Listeners;

/// <summary>
/// Listens to incoming RPC requests to download files.
/// </summary>
public class NxmRpcListener : IDisposable
{
    private readonly ILogger<NxmRpcListener> _logger;
    private readonly BufferBlock<NXMUrlMessage> _urlsBlock;
    private readonly CancellationTokenSource _cancellationSource;

    // Note: Temporary until we implement the UI part.
    private readonly Task _listenTask;
    private readonly DownloadService _downloadService;

    public NxmRpcListener(IMessageConsumer<NXMUrlMessage> nxmUrlMessages, ILogger<NxmRpcListener> logger, DownloadService downloadService)
    {
        _logger = logger;
        _downloadService = downloadService;
        _cancellationSource = new CancellationTokenSource();
        _urlsBlock = new BufferBlock<NXMUrlMessage>();
        _listenTask = Task.Run(ListenAsync);
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
                _ = _downloadService.AddNxmTask(item.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception when downloading a mod");
            }
        }
    }

    /// <summary>
    /// Stops listening for background events.
    /// </summary>
    public void Dispose()
    {
        try { _cancellationSource.Cancel(); }
        catch (Exception) { /* ignored */ }

        try { _listenTask.Wait(); }
        catch (Exception e) { _logger.LogError(e, "Unhandled exception in NXM dispatcher"); }

        _urlsBlock.Complete();
        _cancellationSource.Dispose();
    }
}
