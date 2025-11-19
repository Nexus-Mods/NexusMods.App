using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Explicit loadout snapshots. These aren't really snapshots in the traditional sense, instead they are simply
/// markers on a transaction that indicate that we want this transaction to show up in the undo history.
/// </summary>
[Include<Transaction>]
public partial class LoadoutSnapshot : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutSnapshot";
    
    /// <summary>
    /// Inserted on a transaction when a loadout is reverted to some previous state.
    /// </summary>
    public static readonly ReferencesAttribute<Loadout> Snapshot = new(Namespace, nameof(Snapshot));
}
