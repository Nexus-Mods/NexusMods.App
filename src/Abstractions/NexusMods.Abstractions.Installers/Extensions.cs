using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Extensions for various installer related classes
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Creates a StoredFile from a ModFileTreeSource.
    /// </summary>
    public static TempEntity ToStoredFile(this KeyedBox<RelativePath, ModFileTree> input, GamePath to)
    {
        return input.ToStoredFile(to, null);
    }

    /// <summary>
    /// Convert as LibraryArchiveTree node to a LoadoutFile with the given metadata
    /// </summary>
    public static LoadoutFile.New ToLoadoutFile(this KeyedBox<RelativePath, LibraryArchiveTree> input, LoadoutId loadoutId, LoadoutItemGroupId parent, ITransaction tx, GamePath to)
    {
        var libraryFile = input.Item.LibraryFile.Value;

        return new LoadoutFile.New(tx, out var id)
        {
            LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
            {
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    LoadoutId = loadoutId,
                    IsDisabled = false,
                    Name = input.Item.Value.FileName,
                    ParentId = parent,
                },
                TargetPath = to,
            },
            Hash = libraryFile.Hash,
            Size = libraryFile.Size,
        };
    }

    /// <summary>
    /// Creates a group loadout item for the given library archive. Often used by installers to group files from a single archive together
    /// </summary>
    public static LoadoutItem.New ToGroup(this LibraryArchive.ReadOnly archive, LoadoutId loadout, ITransaction tx, string? name = null)
    {
        var groupId = tx.TempId();
        
        var item = new LoadoutItem.New(tx, groupId)
        {
            LoadoutId = loadout,
            IsDisabled = false,
            Name = name ?? archive.AsLibraryFile().FileName,
        };
        
        var group = new LoadoutItemGroup.New(tx, groupId)
        {
            LoadoutItem = item, 
            IsGroupMarker = true,
        };

        return item;
    }

    /// <summary>
    /// Creates a StoredFile from a ModFileTreeSource.
    /// </summary>
    public static TempEntity ToStoredFile(
        this KeyedBox<RelativePath, ModFileTree> input,
        GamePath to,
        TempEntity? metaData)
    {
        var entity = metaData ?? [];
        entity.Add(File.To, to);
        entity.Add(StoredFile.Hash, input.Item.Hash);
        entity.Add(StoredFile.Size, input.Item.Size);
        return entity;
    }
}
