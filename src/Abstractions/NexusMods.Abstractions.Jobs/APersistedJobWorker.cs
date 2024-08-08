using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Base class for persisted job workers.
/// </summary>
[PublicAPI]
public abstract class APersistedJobWorker<T> : AJobWorker<T>, IPersistedJobWorker
    where T : AJob
{
    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <inheritdoc />
    public abstract IJob LoadJob(PersistedJobState.ReadOnly state);
}
