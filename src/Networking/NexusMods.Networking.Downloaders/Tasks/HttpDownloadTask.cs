using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Networking.Downloaders.Interfaces;
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
        var (name, size) = await GetNameAndSizeAsync(uri);
        
        var temporaryPath = TemporaryFileManager.CreateFile();
        var downloaderState = new DownloaderState.New(tx)
        {
            FriendlyName = string.IsNullOrEmpty(name) ? "<Unknown>" : name,
            Size = size,
            Status = DownloadTaskStatus.Idle,
            DownloadPath = temporaryPath.Path.ToString(),
        };
        
        _ = new HttpDownloadState.New(tx)
        {
            DownloaderState = downloaderState, 
            Uri = uri,
        };
        
        await Init(tx, downloaderState.Id);
    }
    
    protected override async Task Download(AbsolutePath destination, CancellationToken token)
    {
        if (!PersistentState.TryGetAsHttpDownloadState(out var httpState))
            throw new InvalidOperationException("State is not a HttpDownloadState");
        await HttpDownloader.DownloadAsync([httpState.Uri], destination, httpState.DownloaderState.Size, TransientState, token);
    }
}
