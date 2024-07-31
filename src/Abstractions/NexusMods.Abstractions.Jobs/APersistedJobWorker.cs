namespace NexusMods.Abstractions.Jobs;

public abstract class APersistedJobWorker<T> : AJobWorker<T>, IPersistedJobWorker
    where T : AJob
{
    private PersistedJobStateId _stateId;

    /// <inheritdoc />
    public Guid Id => Guid.Parse("17DBF060-5A55-4960-81D6-F99E4CD24702");

    /// <inheritdoc />
    public abstract IJob LoadJob(PersistedJobState.ReadOnly state);
}
