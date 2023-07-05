using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// Extension methods for creating <see cref="IDataReference"/> implementation instances.
/// </summary>
[PublicAPI]
public static class ReferenceExtensions
{
    /// <summary>
    /// Creates a new <see cref="LoadoutReference"/> for the given <see cref="Loadout"/>.
    /// </summary>
    public static LoadoutReference ToReference(this Loadout loadout)
    {
        return new LoadoutReference
        {
            DataId = loadout.LoadoutId,
            DataStoreId = loadout.DataStoreId
        };
    }

    /// <summary>
    /// Creates a new <see cref="ModReference"/> for the given <see cref="Mod"/>.
    /// </summary>
    public static ModReference ToReference(this Mod mod, Loadout loadout)
    {
        return new ModReference
        {
            DataId = new ModCursor(loadout.LoadoutId, mod.Id),
            DataStoreId = mod.DataStoreId
        };
    }

    /// <summary>
    /// Creates a new <see cref="ModFileReference"/> for the given <see cref="AModFile"/>.
    /// </summary>
    public static ModFileReference ToReference(this AModFile modFile)
    {
        return new ModFileReference
        {
            DataId = modFile.Id,
            DataStoreId = modFile.DataStoreId
        };
    }
}
