using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a <see cref="IJobWorker"/> which is partially persisted to the DB.
/// </summary>
public interface IPersistedJobWorker : IJobWorker
{
    /// <summary>
    /// The unique ID of the persisted job worker, this is used to identify the worker in the database
    /// when the job is persisted.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Load the job from the persisted state.
    /// </summary>
    public IJob LoadJob(PersistedJobState.ReadOnly state);
}
