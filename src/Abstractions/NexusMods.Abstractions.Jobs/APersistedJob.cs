namespace NexusMods.Abstractions.Jobs;

public abstract class APersistedJob : AJob, IPersistedJob
{
    protected APersistedJob(MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) : base(progress, group, worker,
        monitor
    )
    {
    }

    public PersistedJobStateId PersistedJobStateId { get; }
}
