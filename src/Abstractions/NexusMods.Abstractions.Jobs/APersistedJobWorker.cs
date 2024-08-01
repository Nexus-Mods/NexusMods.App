namespace NexusMods.Abstractions.Jobs;

public abstract class APersistedJobWorker<T> : AJobWorker<T>, IPersistedJobWorker
    where T : AJob
{
    private PersistedJobStateId _stateId;

    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <inheritdoc />
    public abstract IJob LoadJob(PersistedJobState.ReadOnly state);
}
