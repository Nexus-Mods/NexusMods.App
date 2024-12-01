using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// Extension methods for creating <see cref="IDataReference"/> implementation instances.
/// </summary>
[PublicAPI]
public static class ReferenceExtensions
{
    /// <summary>
    /// Creates a new <see cref="LoadoutReference"/> for the given <see cref="Loadout"/>.
    /// </summary>
    public static LoadoutReference ToReference(this Loadout.ReadOnly loadout)
    {
        return new LoadoutReference
        {
            DataId = loadout.LoadoutId,
            TxId = loadout.Db.BasisTxId,
        };
    }
    
    /// <summary>
    /// Creates a new <see cref="LoadoutItemGroupReference"/> for the given <see cref="LoadoutItemGroup.ReadOnly"/>.
    /// </summary>
    public static LoadoutItemGroupReference ToReference(this LoadoutItemGroup.ReadOnly group, Loadout.ReadOnly loadout)
    {
        return new LoadoutItemGroupReference
        {
            DataId = group.LoadoutItemGroupId,
            TxId = group.Db.BasisTxId,
        };
    }
}
