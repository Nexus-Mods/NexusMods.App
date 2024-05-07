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
    public static LoadoutReference ToReference(this Loadout.Model loadout)
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
    public static ModReference ToReference(this Mod.Model mod, Loadout.Model loadout)
    {
        return new ModReference
        {
            DataId = mod.ModId,
            TxId = mod.Db.BasisTxId,
        };
    }

    /// <summary>
    /// Creates a new <see cref="ModFileReference"/> for the given <see cref="AModFile"/>.
    /// </summary>
    public static ModFileReference ToReference(this File.Model modFile)
    {
        return new ModFileReference
        {
            DataId = modFile.FileId,
            TxId = modFile.Db.BasisTxId,
        };
    }
}
