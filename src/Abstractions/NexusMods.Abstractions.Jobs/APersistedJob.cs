using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Base class for persisted jobs.
/// </summary>
[PublicAPI]
public abstract class APersistedJob : AJob, IPersistedJob
{
    private readonly IConnection _connection;

    /// <inheritdoc />
    protected APersistedJob(
        IConnection connection,
        PersistedJobState.ReadOnly state,
        MutableProgress progress,
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default) : base(progress, group, worker, monitor)
    {
        _connection = connection;

        PersistedJobStateId = state;
        _state = state;
    }

    /// <inheritdoc />
    public PersistedJobStateId PersistedJobStateId { get; }
    
    private PersistedJobState.ReadOnly _state;

    /// <summary>
    /// Get the value of the attribute from the internal persisted job state.
    /// </summary>
    public THighLevel Get<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attr) 
        where THighLevel : notnull
    {
        return attr.Get(_state);
    }
    
    /// <summary>
    /// Get the value of the attribute from the internal persisted job state.
    /// </summary>
    public Values<THighLevel, TLowLevel> Get<THighLevel, TLowLevel>(CollectionAttribute<THighLevel, TLowLevel> attr)
    {
        return attr.Get(_state);
    }
    
    /// <summary>
    /// Get the value of the attribute from the internal persisted job state.
    /// </summary>
    public THighLevel Get<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attr, THighLevel defaultValue) 
        where THighLevel : notnull
    {
        if (!_state.Contains(attr))
            return defaultValue;
        
        return attr.Get(_state);
    }
    
    /// <summary>
    /// Update the value of the attribute in the internal persisted job state.
    /// </summary>
    public async Task Set<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attr, THighLevel value) 
        where THighLevel : notnull
    {
        using var tx = _connection.BeginTransaction();
        attr.Add(tx, PersistedJobStateId, value);
        await tx.Commit();
        _state = _state.Rebase();
    }

    internal override void SetStatus(JobStatus value)
    {
        base.SetStatus(value);
        // TODO: Set(PersistedJobState.Status, value);
    }
}
