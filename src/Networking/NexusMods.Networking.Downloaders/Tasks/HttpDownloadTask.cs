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
    public async Task Create(Uri uri)
    {
        await base.Create();
        using var tx = Connection.BeginTransaction();
        
        // Try to divine the name and size of the download, via HTTP headers
        var (name, size) = await GetNameAndSizeAsync(uri);
        if (!string.IsNullOrEmpty(name))
        {
            tx.Add(PersistentState.Id, DownloaderState.FriendlyName, name);
            tx.Add(PersistentState.Id, DownloaderState.Size, size);
        }
        tx.Add(PersistentState.Id, HttpDownloadState.Uri, uri);
        var result = await tx.Commit();
        PersistentState = result.Remap(PersistentState);
    }
    
    protected override async Task Download(AbsolutePath destination, CancellationToken token)
    {
        var url = PersistentState.Get(HttpDownloadState.Uri);
        var size = PersistentState.Get(DownloaderState.Size);
        await HttpDownloader.DownloadAsync([url], destination, size, TransientState, token);
    }
}
