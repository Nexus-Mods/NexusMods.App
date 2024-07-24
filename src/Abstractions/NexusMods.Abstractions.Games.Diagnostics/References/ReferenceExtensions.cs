using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using File = NexusMods.Abstractions.Loadouts.Files.File;

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
    /// Creates a new <see cref="ModReference"/> for the given <see cref="Mod"/>.
    /// </summary>
    public static ModReference ToReference(this Mod.ReadOnly mod, Loadout.ReadOnly loadout)
    {
        return new ModReference
        {
            DataId = mod.ModId,
            TxId = mod.Db.BasisTxId,
        };
    }
    
    /// <summary>
    /// Creates a new <see cref="ModReference"/> for the given <see cref="Mod"/>.
    /// </summary>
    public static LoadoutItemGroupReference ToReference(this LoadoutItemGroup.ReadOnly group, Loadout.ReadOnly loadout)
    {
        return new LoadoutItemGroupReference
        {
            DataId = group.LoadoutItemGroupId,
            TxId = group.Db.BasisTxId,
        };
    }

    /// <summary>
    /// Creates a new <see cref="ModFileReference"/> for the given <see cref="AModFile"/>.
    /// </summary>
    public static ModFileReference ToReference(this File.ReadOnly modFile)
    {
        return new ModFileReference
        {
            DataId = modFile.FileId,
            TxId = modFile.Db.BasisTxId,
        };
    }
}
