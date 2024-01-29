using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Visitors;

namespace NexusMods.Abstractions.Loadouts.Synchronizers.Transformer;

/// <summary>
/// A transformer that creates a new loadout from a flattened loadout.
/// </summary>
public class FlattenedToLoadoutTransformer : ALoadoutVisitor
{
    private readonly Dictionary<ModId,Mod> _modReplacements;
    private readonly HashSet<GamePath> _toDelete;
    private readonly Dictionary<ModId, List<AModFile>> _moveFrom;
    private readonly Dictionary<ModId, List<AModFile>> _moveTo;
    private readonly Dictionary<(ModId, ModFileId),AModFile> _fileReplacements;

    /// <summary>
    /// Standard contructor.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFlattenedLoadout"></param>
    public FlattenedToLoadoutTransformer(FlattenedLoadout flattenedLoadout, Loadout prevLoadout, FlattenedLoadout prevFlattenedLoadout)
    {

        // The pattern is pretty simple here, we'll preprocess as much information as we can, and construct
        // helper collections to allow us to efficiently transform the loadout. The overall goal is to reduce
        // all operations to O(n) time complexity, where n is the number of files in the loadout.

        _modReplacements = new Dictionary<ModId, Mod>();

        // These are files that no longer exist in the loadout, so we need to delete them
        // TODO: This is inefficient due to double call of GamePath()
        _toDelete = prevFlattenedLoadout.GetAllDescendentFiles()
            .Where(f => !flattenedLoadout.TryGetValue(f.Item.GetGamePath(), out _))
            .Select(f => f.GamePath())
            .ToHashSet();

        _moveFrom = new Dictionary<ModId, List<AModFile>>();
        _moveTo = new Dictionary<ModId, List<AModFile>>();

        void AddToValues(Dictionary<ModId, List<AModFile>> dict, ModId key, AModFile file)
        {
            if (dict.TryGetValue(key, out var list))
            {
                list.Add(file);
            }
            else
            {
                dict.Add(key, new List<AModFile> { file });
            }
        }

        _fileReplacements = new Dictionary<(ModId, ModFileId), AModFile>();

        // These are files that have changed or are new, so we need to add/update them
        foreach (var item in flattenedLoadout.GetAllDescendentFiles())
        {
            var newPair = item.Item.Value;
            var path = item.GamePath();
            if (prevFlattenedLoadout.TryGetValue(path, out var prevPair))
            {
                if (!prevPair.Item.Value!.Mod.Id.Equals(newPair!.Mod.Id))
                    continue;

                if (prevPair.Item.Value!.File.Id.Equals(newPair.File.Id))
                {
                    if (prevPair.Item.Value!.File.DataStoreId.Equals(newPair.File.DataStoreId))
                    {
                        // Nothing to change
                        continue;
                    }
                    _fileReplacements[(newPair.Mod.Id, newPair.File.Id)] = newPair.File;
                }
                else
                {
                    AddToValues(_moveFrom, prevPair.Item.Value.Mod.Id, prevPair.Item.Value.File);
                    AddToValues(_moveTo, newPair.Mod.Id, newPair.File);
                }
            }
            else
            {
                // New file
                if (_modReplacements.TryGetValue(newPair!.Mod.Id, out var mod1))
                {
                    // We've already processed this mod
                    mod1 = mod1 with
                    {
                        Files = mod1.Files.With(newPair.File.Id, newPair.File)
                    };
                    _modReplacements[mod1.Id] = mod1;
                }
                else if (prevLoadout.Mods.TryGetValue(newPair.Mod.Id, out var mod2))
                {
                    // Mod already exists in the loadout, so we can just add the file
                    mod2 = mod2 with
                    {
                        Files = mod2.Files.With(newPair.File.Id, newPair.File)
                    };
                    _modReplacements[mod2.Id] = mod2;
                }
                else
                {
                    // We need to use the mod attached to the pair
                    var mod = newPair.Mod with
                    {
                        Files = newPair.Mod.Files.With(newPair.File.Id, newPair.File)
                    };
                    _modReplacements[mod.Id] = mod;
                }
            }
        }
    }

    protected override Loadout AlterBefore(Loadout loadout)
    {
        // Add in all the new and updated mods
        loadout = loadout with
        {
            Mods = loadout.Mods.With(_modReplacements)
        };
        return base.AlterBefore(loadout);
    }

    protected override Mod? AlterBefore(Loadout loadout, Mod mod)
    {
        if (_moveFrom.TryGetValue(mod.Id, out var replacements))
        {
            mod = mod with
            {
                Files = replacements.Aggregate(mod.Files, (files, toRemove) => files.Without(toRemove.Id))
            };
        }
        if (_moveTo.TryGetValue(mod.Id, out replacements))
        {
            mod = mod with
            {
                Files = mod.Files.With(replacements.Select(m => KeyValuePair.Create(m.Id, m)))
            };
        }

        return mod;
    }

    protected override AModFile? AlterBefore(Loadout loadout, Mod mod, AModFile modFile)
    {
        // Delete any files that no longer exist
        if (modFile is IToFile tf && _toDelete.Contains(tf.To))
        {
            return null;
        }

        // Perform any file replacements
        if (_fileReplacements.TryGetValue((mod.Id, modFile.Id), out var replacement))
        {
            return replacement;
        }

        return modFile;
    }

}
