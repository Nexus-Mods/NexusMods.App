using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Installers;

[PublicAPI]
public static class Extensions
{
    public static LoadoutItemGroup.New ToGroup(
        this LibraryArchive.ReadOnly archive,
        LoadoutId loadout,
        ITransaction tx,
        out LoadoutItem.New loadoutItem,
        string? name = null,
        Optional<EntityId> entityId = default)
    {
        var groupId = entityId.ValueOr(tx.TempId);

        loadoutItem = new LoadoutItem.New(tx, groupId)
        {
            LoadoutId = loadout,
            Name = name ?? archive.AsLibraryFile().FileName,
        };

        var group = new LoadoutItemGroup.New(tx, groupId)
        {
            LoadoutItem = loadoutItem,
            IsIsLoadoutItemGroupMarker = true,
        };

        return group;
    }
}
