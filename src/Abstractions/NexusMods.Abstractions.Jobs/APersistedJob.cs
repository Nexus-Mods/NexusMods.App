using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Jobs;

public abstract class APersistedJob : AJob, IPersistedJob
{
    private readonly IConnection _connection;

    /// <inheritdoc />
    protected APersistedJob(IConnection connection, PersistedJobStateId id, MutableProgress progress, IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default) : 
        base(progress, group, worker, monitor)
    {
        _connection = connection;
        PersistedJobStateId = id;
        _state = PersistedJobState.Load(_connection.Db, PersistedJobStateId);
    }

    /// <inheritdoc />
    public PersistedJobStateId PersistedJobStateId { get; }
    
    private PersistedJobState.ReadOnly _state;

    /// <summary>
    /// Get the value of the attribute from the internal persisted job state.
    /// </summary>
    public THighLevel Get<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attr)
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
    {
        if (!_state.Contains(attr))
            return defaultValue;
        
        return attr.Get(_state);
    }
    
    /// <summary>
    /// Update the value of the attribute in the internal persisted job state.
    /// </summary>
    public async Task Set<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attr, THighLevel value)
    {
        using var tx = _connection.BeginTransaction();
        attr.Add(tx, PersistedJobStateId, value);
        await tx.Commit();
        _state = _state.Rebase();
    }
}
