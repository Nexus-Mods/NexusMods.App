using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
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
    /// <param name="input"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static StoredFile ToStoredFile(this KeyedBox<RelativePath, ModFileTree> input, GamePath to)
    {
        return new StoredFile
        {
            Id = ModFileId.NewId(),
            To = to,
            Hash = input.Item.Hash,
            Size = input.Item.Size
        };
    }

}
