using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddTask(NexusMods.Abstractions.NexusWebApi.Types.NXMUrl)"/>.
/// </remarks>
public class HttpDownloadTask(IServiceProvider provider) : ADownloadTask(provider)
{
    /// <summary>
    /// Creates a new download task for the given URI.
    /// </summary>
    /// <param name="uri"></param>
    public async Task Create(Uri uri)
    {
        using var tx = Connection.BeginTransaction();
        var id = base.Create(tx);
        
        // Try to divine the name and size of the download, via HTTP headers
        var (name, size) = await GetNameAndSizeAsync(uri);
        if (!string.IsNullOrEmpty(name))
        {
            tx.Add(id, DownloaderState.FriendlyName, name);
            tx.Add(id, DownloaderState.Size, size);
        }
        tx.Add(id, HttpDownloadState.Uri, uri);
        await Init(tx, id);
    }
    
    protected override async Task Download(AbsolutePath destination, CancellationToken token)
    {
        var url = PersistentState.Get(HttpDownloadState.Uri);
        var size = PersistentState.Get(DownloaderState.Size);
        await HttpDownloader.DownloadAsync([url], destination, size, TransientState, token);
    }
}
