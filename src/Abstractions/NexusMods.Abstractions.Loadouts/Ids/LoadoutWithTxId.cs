using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts.Ids;

/// <summary>
/// A combination of a loadout id and a transaction id.
/// </summary>
/// <param name="Id"></param>
/// <param name="Tx"></param>
public record struct LoadoutWithTxId(LoadoutId Id, TxId Tx);
