using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

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
    
    /// <summary>
    /// Convert as LibraryArchiveTree node to a LoadoutFile with the given metadata
    /// </summary>
    public static LoadoutFile.New ToLoadoutFile(
        this KeyedBox<RelativePath, LibraryArchiveTree> input,
        LoadoutId loadoutId,
        LoadoutItemGroupId parent,
        ITransaction tx,
        GamePath to,
        Optional<EntityId> entityId = default)
    {
        var id = entityId.ValueOr(tx.TempId);
        var libraryFile = input.Item.LibraryFile.Value;

        return new LoadoutFile.New(tx, id)
        {
            LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
            {
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    LoadoutId = loadoutId,
                    IsIsDisabledMarker = false,
                    Name = input.Item.Value.FileName,
                    ParentId = parent,
                },
                TargetPath = to,
            },
            Hash = libraryFile.Hash,
            Size = libraryFile.Size,
        };
    }
}
