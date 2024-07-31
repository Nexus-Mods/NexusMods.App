using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

public class HttpDownloadJob : APersistedJob
{
    public HttpDownloadJob(IConnection connection, PersistedJobStateId id, MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) 
        : base(connection, id, progress, group, worker, monitor)
    {
        
    }
    
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
}
