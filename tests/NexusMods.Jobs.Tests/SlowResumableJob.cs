using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs.Tests;

public class SlowResumableJob : AJob, IPersistedJob
{
    public SlowResumableJob(MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) : base(progress, group, worker,
        monitor
    )
    {
    }

    public PersistedJobStateId PersistedJobStateId { get; }
    public static IPersistedJob ToJob(PersistedJobState.ReadOnly jobState)
    {
        throw new NotImplementedException();
    }
}
