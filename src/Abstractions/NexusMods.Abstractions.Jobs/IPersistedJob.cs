using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a <see cref="IJob"/> which is partially persisted to the DB.
/// </summary>
[PublicAPI]
public interface IPersistedJob : IJob
{
    /// <summary>
    /// Gets the ID of the persisted job state.
    /// </summary>
    PersistedJobStateId PersistedJobStateId { get; }

    static abstract IPersistedJob ToJob(PersistedJobState.ReadOnly jobState);
}
