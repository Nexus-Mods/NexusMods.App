using System.Diagnostics;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts.Files;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Provides utility methods for diffing DiskStates and loadouts
/// </summary>
public static class LoadoutSynchronizerDiffExtensions
{
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="diskState">The old state, e.g. last applied DiskState</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    public static async ValueTask<FileDiffTree> LoadoutToDiskDiff(
        this IStandardizedLoadoutSynchronizer loadoutSynchronizer,
        Loadout loadout,
        DiskStateTree diskState)
    {
        var flattenedLoadout = await loadoutSynchronizer.LoadoutToFlattenedLoadout(loadout);
        return await FlattenedLoadoutToDiskDiff(flattenedLoadout, diskState);
    }

    private static ValueTask<FileDiffTree> FlattenedLoadoutToDiskDiff(FlattenedLoadout flattenedLoadout, DiskStateTree diskState)
    {
        var loadoutFiles = flattenedLoadout.GetAllDescendentFiles();
        var diskStateEntries = diskState.GetAllDescendentFiles();

        Dictionary<GamePath, DiskDiffEntry> resultingItems = new();

        // Add all the disk state entries to the result, checking for changes
        foreach (var diskItem in diskStateEntries)
        {
            var gamePath = diskItem.GamePath();
            if (flattenedLoadout.TryGetValue(gamePath, out var loadoutFileEntry))
            {
                switch (loadoutFileEntry.Item.Value.File)
                {
                    case StoredFile sf:
                        if (sf.Hash != diskItem.Item.Value.Hash)
                        {
                            resultingItems.Add(gamePath,
                                new DiskDiffEntry
                                {
                                    GamePath = gamePath,
                                    Hash = sf.Hash,
                                    Size = sf.Size,
                                    ChangeType = FileChangeType.Modified,
                                }
                            );
                        }
                        else
                        {
                            resultingItems.Add(gamePath,
                                new DiskDiffEntry
                                {
                                    GamePath = gamePath,
                                    Hash = sf.Hash,
                                    Size = sf.Size,
                                    ChangeType = FileChangeType.None,
                                }
                            );
                        }

                        break;
                    case IGeneratedFile gf and IToFile:
                        // TODO: Implement change detection for generated files
                        break;
                    default:
                        throw new UnreachableException("No way to handle this file");
                }
            }
            else
            {
                resultingItems.Add(gamePath,
                    new DiskDiffEntry
                    {
                        GamePath = gamePath,
                        Hash = diskItem.Item.Value.Hash,
                        Size = diskItem.Item.Value.Size,
                        ChangeType = FileChangeType.Removed,
                    }
                );
            }
        }

        // Add all the new files to the result
        foreach (var loadoutFile in loadoutFiles)
        {
            var gamePath = loadoutFile.GamePath();
            switch (loadoutFile.Item.Value.File)
            {
                case StoredFile sf:
                    if (!resultingItems.TryGetValue(gamePath, out _))
                    {
                        resultingItems.Add(gamePath,
                            new DiskDiffEntry
                            {
                                GamePath = gamePath,
                                Hash = sf.Hash,
                                Size = sf.Size,
                                ChangeType = FileChangeType.Added,
                            }
                        );
                    }

                    break;
                case IGeneratedFile gf and IToFile:
                    // TODO: Implement change detection for generated files
                    break;
                default:
                    throw new UnreachableException("No way to handle this file");
            }
        }

        return ValueTask.FromResult(FileDiffTree.Create(resultingItems));
    }
}
