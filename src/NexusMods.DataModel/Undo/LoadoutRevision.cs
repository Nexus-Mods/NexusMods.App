using NexusMods.Cascade;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.DataModel.Undo;

/// <summary>
/// Row definition for the LoadoutRevision with the current TxEntity and the previous TxEntity.
/// </summary>
public partial record struct LoadoutRevision(EntityId EntityId, EntityId TxEntity, EntityId PrevTxEntity) : IRowDefinition;

public partial record struct LoadoutRevisionWithStats(EntityId LoadoutId, EntityId TxId, int Added, int Removed, int Modified, DateTimeOffset Timestamp) : IRowDefinition;
