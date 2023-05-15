using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// All logic for synchronizing loadouts with the game folders is contained within this class.
/// </summary>
public class LoadoutSyncronizer
{
    private readonly IFingerprintCache<Mod,CachedModSortRules> _modSortRulesFingerprintCache;

    public LoadoutSyncronizer(IFingerprintCache<Mod, CachedModSortRules> modSortRulesFingerprintCache)
    {
        _modSortRulesFingerprintCache = modSortRulesFingerprintCache;
    }


    /// <summary>
    /// Flattens a loadout into a dictionary of <see cref="GamePath"/> to <see cref="AModFile"/>, any files that are
    /// not IToFile are ignored.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public async ValueTask<IReadOnlyDictionary<GamePath, AModFile>> FlattenLoadout(Loadout loadout)
    {
        var dict = new Dictionary<GamePath, AModFile>();

        var sorted = await SortMods(loadout);

        foreach (var mod in sorted)
        {
            foreach (var (_, file) in mod.Files)
            {
                if (file is not IToFile toFile)
                    continue;

                dict[toFile.To] = file;
            }
        }
        return dict;
    }


    /// <summary>
    /// Sorts the mods in the given loadout, using the rules defined in the mods.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Mod>> SortMods(Loadout loadout)
    {
        var modRules = await loadout.Mods.Values
            .SelectAsync(async mod => (mod.Id, await ModSortRules(loadout, mod).ToListAsync()))
            .ToDictionaryAsync(r => r.Id, r => r.Item2);
        var sorted = Sorter.Sort<Mod, ModId>(loadout.Mods.Values.ToList(), m => m.Id, m => modRules[m.Id]);
        return sorted;
    }

    /// <summary>
    /// Generates a list of Rules for sorting the given mod, if the mod has any generated rules then they are
    /// calculated and returned.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="mod"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<ISortRule<Mod, ModId>> ModSortRules(Loadout loadout, Mod mod)
    {
        foreach (var rule in mod.SortRules)
        {
            if (rule is IGeneratedSortRule gen)
            {
                var fingerprint = gen.TriggerFilter.GetFingerprint(mod.Id, loadout);
                if (_modSortRulesFingerprintCache.TryGet(fingerprint, out var cached))
                {
                    foreach (var cachedRule in cached.Rules)
                        yield return cachedRule;
                    continue;
                }

                var rules = await gen.GenerateSortRules(mod.Id, loadout).ToArrayAsync();
                _modSortRulesFingerprintCache.Set(fingerprint, new CachedModSortRules
                {
                    Rules = rules
                });

                foreach (var genRule in rules)
                {
                    yield return genRule;
                }
            }
            else
            {
                yield return rule;
            }
        }
    }

}
