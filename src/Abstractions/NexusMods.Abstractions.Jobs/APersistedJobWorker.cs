namespace NexusMods.Abstractions.Jobs;

public class APersistedJobWorker<T> : AJobWorker<T>
    where T : AJob
{
    private PersistedJobStateId _stateId;
    
    protected override Task<JobResult> ExecuteAsync(T job, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
