using System.Collections.Concurrent;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// All logic for synchronizing loadouts with the game folders is contained within this class.
/// </summary>
public class LoadoutSyncronizer
{
    private readonly IFingerprintCache<Mod,CachedModSortRules> _modSortRulesFingerprintCache;
    private readonly IDirectoryIndexer _directoryIndexer;
    private readonly IArchiveManager _archiveManager;
    private readonly IFingerprintCache<IGeneratedFile,CachedGeneratedFileData> _generatedFileFingerprintCache;

    public LoadoutSyncronizer(IFingerprintCache<Mod, CachedModSortRules> modSortRulesFingerprintCache, 
        IDirectoryIndexer directoryIndexer, 
        IArchiveManager archiveManager,
        IFingerprintCache<IGeneratedFile, CachedGeneratedFileData> generatedFileFingerprintCache)
    {
        _archiveManager = archiveManager;
        _generatedFileFingerprintCache = generatedFileFingerprintCache;
        _modSortRulesFingerprintCache = modSortRulesFingerprintCache;
        _directoryIndexer = directoryIndexer;
    }


    /// <summary>
    /// Flattens a loadout into a dictionary of files and their corresponding mods. Any files that are not
    /// IToFile will be ignored.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public async ValueTask<IReadOnlyDictionary<GamePath, (AModFile File, Mod Mod)>> FlattenLoadout(Loadout loadout)
    {
        var dict = new Dictionary<GamePath, (AModFile, Mod)>();

        var sorted = await SortMods(loadout);

        foreach (var mod in sorted)
        {
            foreach (var (_, file) in mod.Files)
            {
                if (file is not IToFile toFile)
                    continue;

                dict[toFile.To] = (file, mod);
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

    public async IAsyncEnumerable<IApplyStep> MakeApplySteps(Loadout loadout, CancellationToken token = default)
    {

        var install = loadout.Installation;

        var existingFiles = _directoryIndexer.IndexFolders(install.Locations.Values, token);

        var flattenedLoadout = await FlattenLoadout(loadout);
        
        var seen = new ConcurrentBag<GamePath>();

        await foreach (var existing in existingFiles.WithCancellation(token))
        {
            var gamePath = install.ToGamePath(existing.Path);
            seen.Add(gamePath);
            
            if (flattenedLoadout.TryGetValue(gamePath, out var planFile))
            {
                var planMetadata = await GetMetaData(planFile.File, existing.Path);
                if (planMetadata is null || planMetadata.Hash != existing.Hash || planMetadata.Size != existing.Size)
                {
                    await foreach (var itm in EmitReplacePlan(existing, loadout, planFile.File, planFile.Mod).WithCancellation(token))
                    {
                        yield return itm;
                    }
                }
            }
            else
            {
                await foreach(var itm in EmitDeletePlan(existing, loadout).WithCancellation(token))
                {
                    yield return itm;
                }
            }
        }
        
        foreach (var (gamePath, (modFile, mod)) in flattenedLoadout.Where(kv => !seen.Contains(kv.Key)))
        {
            var absPath = gamePath.CombineChecked(install);
            
            if (seen.Contains(gamePath))
                continue;

            await foreach (var itm in EmitCreatePlan(modFile, mod, loadout, absPath).WithCancellation(token))
            {
                yield return itm;
            }
        }
        
    }

    private async IAsyncEnumerable<IApplyStep> EmitCreatePlan(AModFile modFile, Mod mod, Loadout loadout, AbsolutePath absPath)
    {
        // If the file is from an archive, then we can just extract it
        if (modFile is IFromArchive fromArchive)
        {
            yield return new ExtractFile
            {
                To = absPath,
                Hash = fromArchive.Hash,
                Size = fromArchive.Size
            };
            yield break;
        }

        // If the file is generated
        if (modFile is IGeneratedFile generatedFile)
        {
            // Get the fingerprint for the generated file
            var fingerprint = generatedFile.TriggerFilter.GetFingerprint((mod.Id, modFile.Id), loadout);
            if (_generatedFileFingerprintCache.TryGet(fingerprint, out var cached))
            {
                // If we have a cached version of the file in an archive, then we can just extract it
                if (await _archiveManager.HaveFile(cached.Hash))
                {
                    yield return new ExtractFile
                    {
                        To = absPath,
                        Hash = cached.Hash,
                        Size = cached.Size
                    };
                    yield break;
                }
            }
            
            // Otherwise we need to generate the file
            yield return new GenerateFile
            {
                To = absPath,
                Source = generatedFile,
                Fingerprint = fingerprint
            };
            yield break;
        }

        // This should never happen
        throw new NotImplementedException();
    }

    private async IAsyncEnumerable<IApplyStep> EmitDeletePlan(HashedEntry existing, Loadout loadout)
    {
        if (!await _archiveManager.HaveFile(existing.Hash))
        {
            yield return new BackupFile
            {
                Hash = existing.Hash,
                Size = existing.Size,
                To = existing.Path
            };
        }


        yield return new DeleteFile
        {
            To = existing.Path,
            Hash = existing.Hash,
            Size = existing.Size
        };
    }

    private async IAsyncEnumerable<IApplyStep> EmitReplacePlan(HashedEntry existing, Loadout loadout, AModFile planFile, Mod mod)
    {
        await foreach (var itm in EmitDeletePlan(existing, loadout))
        {
            yield return itm;
        }
        
        await foreach (var itm in EmitCreatePlan(planFile, mod, loadout, existing.Path))
        {
            yield return itm;
        }
    }

    public async ValueTask<FileMetaData?> GetMetaData(AModFile file, AbsolutePath path)
    {
        if (file is IFromArchive fa)
            return new FileMetaData(path, fa.Hash, fa.Size);
        throw new NotImplementedException();
    }


    public record FileMetaData(AbsolutePath Path, Hash Hash, Size Size);
    
}
