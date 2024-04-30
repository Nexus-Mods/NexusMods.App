using System.Collections.Immutable;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

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
    /// Creates a StoredFile from a ModFileTreeSource.
    /// </summary>
    public static TempEntity ToStoredFile(
        this KeyedBox<RelativePath, ModFileTree> input,
        GamePath to,
        TempEntity? metaData)
    {
        var entity = metaData ?? [];
        entity.Add(StoredFile.To, to);
        entity.Add(StoredFile.Hash, input.Item.Hash);
        entity.Add(StoredFile.Size, input.Item.Size);
        return entity;
    }
}
