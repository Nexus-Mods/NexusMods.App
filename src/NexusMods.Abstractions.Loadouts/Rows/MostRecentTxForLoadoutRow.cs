using NexusMods.Cascade;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts.Rows;

public readonly partial record struct MostRecentTxForLoadoutRow(EntityId LoadoutId, EntityId TxId) : IRowDefinition;
