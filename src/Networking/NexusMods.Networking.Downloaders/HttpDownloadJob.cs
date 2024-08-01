using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// Job for downloading files from the internet.
/// </summary>
public class HttpDownloadJob : APersistedJob, IHttpDownloadJob
{
    /// <inheritdoc />
    public HttpDownloadJob(IConnection connection, PersistedJobStateId id, MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) 
        : base(connection, id, progress, group, worker, monitor)
    {
        
    }
    
    /// <summary>
    /// Destination path for the downloaded file.
    /// </summary>
    public AbsolutePath DownloadPath => Get(HttpDownloadJobPersistedState.Destination);
    
    /// <summary>
    /// The URI of the file to download.
    /// </summary>
    public Uri DownloadUri => Get(HttpDownloadJobPersistedState.Uri);
    
    public static async Task<IJob> Create(IConnection connection, MutableProgress progress, HttpDownloadJobWorker worker, Uri uri, AbsolutePath destination)
    {
        using var tx = connection.BeginTransaction(); 
        
        _ = new HttpDownloadJobPersistedState.New(tx, out var id)
        {
            PersistedJobState = new PersistedJobState.New(tx, id)
            {
                Status = JobStatus.Created,
                Worker = worker,
            },
            Uri = uri,
            Destination = destination,
        };

        var results = await tx.Commit();
        return new HttpDownloadJob(connection, results[id], progress, worker: worker);
    }
    
    /// <inheritdoc />
    public async ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        throw new NotImplementedException();
    }
}
