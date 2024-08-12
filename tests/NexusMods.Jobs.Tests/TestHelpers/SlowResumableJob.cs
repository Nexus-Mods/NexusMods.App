using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs.Tests.TestHelpers;

public class SlowResumableJob : APersistedJob
{
    internal SlowResumableJob(
        IConnection connection,
        PersistedJobState.ReadOnly persistedJobState,
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default) : base(connection, persistedJobState, null!, group, worker, monitor) { }

    public static async Task<IJob> Create(IConnection connection, SlowResumableJobWorker worker, ulong maxCount)
    {
        using var tx = connection.BeginTransaction(); 
        
        var newState = new SlowResumableJobPersistedState.New(tx, out var id)
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
        return new SlowResumableJob(connection, results.Remap(newState).AsPersistedJobState(), worker: worker);
    }
}
