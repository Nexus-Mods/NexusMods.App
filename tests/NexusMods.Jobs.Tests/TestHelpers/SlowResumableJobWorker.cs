using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs.Tests.TestHelpers;

public class SlowResumableJobWorker : APersistedJobWorker<SlowResumableJob>
{
    public override Guid Id => new("d4b3b3b3-0b3b-4b3b-8b3b-0b3b3b3b3b3b");
    
    protected override async Task<JobResult> ExecuteAsync(SlowResumableJob job, CancellationToken cancellationToken)
    {
        var current = job.Get(SlowResumableJobPersistedState.Current, 0UL);
        
        while (current < job.Get(SlowResumableJobPersistedState.Max))
        {
            await job.Set(SlowResumableJobPersistedState.Current, current);
            await Task.Delay(25, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            current++;
        }
        
        return JobResult.CreateCompleted(current);
    }

    public override IJob LoadJob(PersistedJobState.ReadOnly state)
    {
        return new SlowResumableJob(state.Db.Connection, state, worker: this);
    }
}
