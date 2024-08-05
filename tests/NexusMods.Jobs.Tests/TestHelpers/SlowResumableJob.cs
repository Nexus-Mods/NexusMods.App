using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs.Tests.TestHelpers;

public class SlowResumableJob : APersistedJob
{
    internal SlowResumableJob(IConnection connection, PersistedJobStateId persistedJobStateId, MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) :
        base(connection, persistedJobStateId, progress, group, worker, monitor)
    {
    }

    /// <summary>
    /// Creates a new SlowResumableJob.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="progress"></param>
    /// <param name="worker"></param>
    /// <returns></returns>
    public static async Task<IJob> Create(IConnection connection, MutableProgress progress, SlowResumableJobWorker worker, ulong maxCount)
    {
        using var tx = connection.BeginTransaction(); 
        
        _ = new SlowResumableJobPersistedState.New(tx, out var id)
        {
            PersistedJobState = new PersistedJobState.New(tx, id)
            {
                Status = JobStatus.Created,
                Worker = worker,
            },
            Current = 0,
            Max = maxCount,
        };

        var results = await tx.Commit();
        return new SlowResumableJob(connection, results[id], progress, worker: worker);
    }
}
