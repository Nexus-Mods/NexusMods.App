using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.IngestSteps;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;
using BackupFile = NexusMods.DataModel.Loadouts.ApplySteps.BackupFile;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// All logic for synchronizing loadouts with the game folders is contained within this class.
/// </summary>
public class LoadoutSynchronizer
{
    private readonly IFingerprintCache<Mod,CachedModSortRules> _modSortRulesFingerprintCache;
    private readonly IDirectoryIndexer _directoryIndexer;
    private readonly IArchiveManager _archiveManager;
    private readonly IFingerprintCache<IGeneratedFile,CachedGeneratedFileData> _generatedFileFingerprintCache;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly ILogger<LoadoutSynchronizer> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="modSortRulesFingerprintCache"></param>
    /// <param name="directoryIndexer"></param>
    /// <param name="archiveManager"></param>
    /// <param name="generatedFileFingerprintCache"></param>
    /// <param name="loadoutRegistry"></param>
    public LoadoutSynchronizer(ILogger<LoadoutSynchronizer> logger, 
        IFingerprintCache<Mod, CachedModSortRules> modSortRulesFingerprintCache,
        IDirectoryIndexer directoryIndexer,
        IArchiveManager archiveManager,
        IFingerprintCache<IGeneratedFile, CachedGeneratedFileData> generatedFileFingerprintCache,
        LoadoutRegistry loadoutRegistry)
    {
        _logger = logger;
        _archiveManager = archiveManager;
        _generatedFileFingerprintCache = generatedFileFingerprintCache;
        _modSortRulesFingerprintCache = modSortRulesFingerprintCache;
        _directoryIndexer = directoryIndexer;
        _loadoutRegistry = loadoutRegistry;
    }


    /// <summary>
    /// Flattens a loadout into a dictionary of files and their corresponding mods. Any files that are not
    /// IToFile will be ignored.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public async ValueTask<(IReadOnlyDictionary<GamePath, ModFilePair> Files, IEnumerable<Mod> Mods)> FlattenLoadout(Loadout loadout)
    {
        var dict = new Dictionary<GamePath, ModFilePair>();

        var sorted = (await SortMods(loadout)).ToList();

        foreach (var mod in sorted)
        {
            foreach (var (_, file) in mod.Files)
            {
                if (file is not IToFile toFile)
                    continue;

                dict[toFile.To] = new ModFilePair {Mod = mod, File = file};
            }
        }
        return (dict, sorted);
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

    /// <summary>
    /// Generates a list of steps that need to be taken to synchronize the given loadout with the game folders.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<ApplyPlan> MakeApplySteps(Loadout loadout, CancellationToken token = default)
    {

        var install = loadout.Installation;

        var existingFiles = _directoryIndexer.IndexFolders(install.Locations.Values, token);

        var (flattenedLoadout, sortedMods) = await FlattenLoadout(loadout);

        var seen = new ConcurrentBag<GamePath>();

        var plan = new List<IApplyStep>();

        var tmpPlan = new Plan
        {
            Loadout = loadout,
            Flattened = flattenedLoadout,
            Mods = Array.Empty<Mod>(),
        };

        await foreach (var existing in existingFiles.WithCancellation(token))
        {
            var gamePath = install.ToGamePath(existing.Path);
            seen.Add(gamePath);

            if (flattenedLoadout.TryGetValue(gamePath, out var planned))
            {
                var planMetadata = await GetMetaData(planned.File, existing.Path);
                if (planMetadata is null || planMetadata.Hash != existing.Hash || planMetadata.Size != existing.Size)
                {
                    await EmitReplacePlan(plan, existing, tmpPlan, planned);
                }
            }
            else
            {
                await EmitDeletePlan(plan, existing);
            }
        }

        foreach (var (gamePath, pair) in flattenedLoadout.Where(kv => !seen.Contains(kv.Key)))
        {
            var absPath = gamePath.CombineChecked(install);

            if (seen.Contains(gamePath))
                continue;

            await EmitCreatePlan(plan, pair, tmpPlan, absPath);
        }

        return new ApplyPlan
        {
            Steps = plan,
            Mods = sortedMods,
            Flattened = flattenedLoadout,
            Loadout = loadout
        };
    }

    private async ValueTask EmitCreatePlan(List<IApplyStep> plan, ModFilePair pair, Plan tmpPlan, AbsolutePath absPath)
    {
        // If the file is from an archive, then we can just extract it
        if (pair.File is IFromArchive fromArchive)
        {
            plan.Add(new ExtractFile
            {
                To = absPath,
                Hash = fromArchive.Hash,
                Size = fromArchive.Size
            });
            return;
        }
        
        // If the file is generated
        if (pair.File is IGeneratedFile generatedFile)
        {
            // Get the fingerprint for the generated file
            var fingerprint = generatedFile.TriggerFilter.GetFingerprint(pair, tmpPlan);
            if (_generatedFileFingerprintCache.TryGet(fingerprint, out var cached))
            {
                // If we have a cached version of the file in an archive, then we can just extract it
                if (await _archiveManager.HaveFile(cached.Hash))
                {
                    plan.Add(new ExtractFile
                    {
                        To = absPath,
                        Hash = cached.Hash,
                        Size = cached.Size
                    });
                    return;
                }
            }

            // Otherwise we need to generate the file
            plan.Add(new GenerateFile
            {
                To = absPath,
                Source = generatedFile,
                Fingerprint = fingerprint
            });
            return;
        }

        // This should never happen
        _logger.LogError("Unknown file type {Type}", pair.File.GetType().Name);
        throw new UnreachableException();
    }

    private async ValueTask EmitDeletePlan(List<IApplyStep> plan, HashedEntry existing)
    {
        if (!await _archiveManager.HaveFile(existing.Hash))
        {
            plan.Add(new BackupFile
            {
                Hash = existing.Hash,
                Size = existing.Size,
                To = existing.Path
            });
        }

        plan.Add(new DeleteFile
        {
            To = existing.Path,
            Hash = existing.Hash,
            Size = existing.Size
        });
    }

    private async ValueTask EmitReplacePlan(List<IApplyStep> plan, HashedEntry existing, Plan tmpPlan, ModFilePair pair)
    {
        await EmitDeletePlan(plan, existing);
        await EmitCreatePlan(plan, pair, tmpPlan, existing.Path);
    }

    /// <summary>
    /// Gets the metadata for the given file, if the file is from an archive then the metadata is returned
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ValueTask<FileMetaData?> GetMetaData(AModFile file, AbsolutePath path)
    {
        if (file is IFromArchive fa)
            return ValueTask.FromResult<FileMetaData?>(new FileMetaData(path, fa.Hash, fa.Size));
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compares the game folders to the loadout and returns a plan of what needs to be done to make the loadout match the game folders
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="modSelector"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<IngestPlan> MakeIngestPlan(Loadout loadout, Func<AbsolutePath, ModId> modSelector, CancellationToken token = default)
    {
        var install = loadout.Installation;

        var existingFiles = _directoryIndexer.IndexFolders(install.Locations.Values, token);

        var (flattenedLoadout, sortedMods) = await FlattenLoadout(loadout);

        var seen = new ConcurrentBag<GamePath>();
        var plan = new List<IIngestStep>();

        await foreach (var existing in existingFiles.WithCancellation(token))
        {
            var gamePath = install.ToGamePath(existing.Path);
            seen.Add(gamePath);

            if (flattenedLoadout.TryGetValue(gamePath, out var planFile))
            {
                var planMetadata = await GetMetaData(planFile.File, existing.Path);
                if (planMetadata == null || planMetadata.Hash != existing.Hash || planMetadata.Size != existing.Size)
                {
                    await EmitIngestReplacePlan(plan, planFile, existing);
                }
                continue;
            }

            await EmitIngestCreatePlan(plan, existing, modSelector);
        }

        foreach (var (gamePath, pair) in flattenedLoadout)
        {
            if (seen.Contains(gamePath))
                continue;

            var absPath = gamePath.CombineChecked(install);

            await EmitRemoveFromLoadout(plan, absPath);
        }

        return new IngestPlan
        {
            Steps = plan,
            Mods = sortedMods,
            Flattened = flattenedLoadout,
            Loadout = loadout
        };
    }

    private ValueTask EmitRemoveFromLoadout(List<IIngestStep> plan, AbsolutePath absPath)
    {
        plan.Add(new IngestSteps.RemoveFromLoadout
        {
            Source = absPath
        });
        return ValueTask.CompletedTask;
    }

    private async ValueTask EmitIngestCreatePlan(List<IIngestStep> plan, HashedEntry existing, Func<AbsolutePath, ModId> modSelector)
    {
        if (!await _archiveManager.HaveFile(existing.Hash))
        {
            plan.Add(new IngestSteps.BackupFile
            {
                Source = existing.Path,
                Hash = existing.Hash,
                Size = existing.Size
            });
        }

        plan.Add(new CreateInLoadout
        {
            Source = existing.Path,
            Hash = existing.Hash,
            Size = existing.Size,
            ModId = modSelector(existing.Path)
        });
    }

    private async ValueTask EmitIngestReplacePlan(List<IIngestStep> plan, ModFilePair pair, HashedEntry existing)
    {
        if (!await _archiveManager.HaveFile(existing.Hash))
        {
            plan.Add(new IngestSteps.BackupFile
            {
                Source = existing.Path,
                Hash = existing.Hash,
                Size = existing.Size
            });
        }

        plan.Add(new ReplaceInLoadout
        {
            Source = existing.Path,
            Hash = existing.Hash,
            Size = existing.Size,
            ModFileId = pair.File.Id,
            ModId = pair.Mod.Id
        });
    }

    /// <summary>
    /// Applies the given steps to the game folder
    /// </summary>
    /// <param name="plan"></param>
    /// <param name="token"></param>
    public async Task Apply(ApplyPlan plan, CancellationToken token = default)
    {
        // Step 1: Backup Files
        var byType = plan.Steps.ToLookup(g => g.GetType());

        var backups = byType[typeof(BackupFile)]
            .OfType<BackupFile>()
            .Select(f => ((IStreamFactory)new NativeFileStreamFactory(f.To), f.Hash, f.Size))
            .ToList();

        if (backups.Any())
            await _archiveManager.BackupFiles(backups, token);

        // Step 2: Delete Files
        foreach (var file in byType[typeof(DeleteFile)].OfType<DeleteFile>())
        {
            file.To.Delete();
        }

        // Step 3: Extract Files
        var extractedFiles = byType[typeof(ExtractFile)]
            .OfType<ExtractFile>()
            .Select(f => (f.Hash, f.To));
        await _archiveManager.ExtractFiles(extractedFiles, token);

        // Step 4: Write Generated Files
        var generatedFiles = byType[typeof(GenerateFile)].OfType<GenerateFile>();
        foreach (var file in generatedFiles)
        {
            var absPath = file.To;
            var dir = absPath.Parent;
            if (!dir.DirectoryExists())
                dir.CreateDirectory();

            await using var stream = absPath.Create();
            var hash = await file.Source.GenerateAsync(stream, plan, token);
            _generatedFileFingerprintCache.Set(file.Fingerprint, new CachedGeneratedFileData
            {
                Hash = hash,
                Size = Size.FromLong(stream.Length)
            });
        }
    }

    /// <summary>
    /// Run an ingest plan
    /// </summary>
    /// <param name="plan"></param>
    /// <param name="commitMessage"></param>
    public async ValueTask<Loadout> Ingest(IngestPlan plan, string commitMessage = "Ingested Changes")
    {
        var byType = plan.Steps.ToLookup(t => t.GetType());
        var backupFiles = byType[typeof(IngestSteps.BackupFile)]
            .OfType<IngestSteps.BackupFile>()
            .Select(f => ((IStreamFactory)new NativeFileStreamFactory(f.Source), f.Hash, f.Size));
        await _archiveManager.BackupFiles(backupFiles);

        return _loadoutRegistry.Alter(plan.Loadout.LoadoutId, commitMessage, new IngestVisitor(byType, plan));
    }

    private class IngestVisitor : ALoadoutVisitor
    {
        private readonly IngestPlan _plan;
        private readonly HashSet<GamePath> _removeFiles;
        private readonly ILookup<ModId,FromArchive> _replaceFiles;
        private readonly ILookup<ModId,FromArchive> _createFiles;

        public IngestVisitor(ILookup<Type, IIngestStep> steps, IngestPlan plan)
        {
            _plan = plan;
            _removeFiles = steps[typeof(IngestSteps.RemoveFromLoadout)]
                .OfType<IngestSteps.RemoveFromLoadout>()
                .Select(r => plan.Loadout.Installation.ToGamePath(r.Source))
                .ToHashSet();

            _replaceFiles = steps[typeof(IngestSteps.ReplaceInLoadout)]
                .OfType<IngestSteps.ReplaceInLoadout>()
                .ToLookup(r => r.ModId,
                    r => new FromArchive
                    {
                        To = plan.Loadout.Installation.ToGamePath(r.Source),
                        Hash = r.Hash,
                        Size = r.Size,
                        Id = r.ModFileId
                    });

            _createFiles = steps[typeof(IngestSteps.CreateInLoadout)]
                .OfType<IngestSteps.CreateInLoadout>()
                .ToLookup(r => r.ModId,
                    r => new FromArchive
                    {
                        To = plan.Loadout.Installation.ToGamePath(r.Source),
                        Hash = r.Hash,
                        Size = r.Size,
                        Id = ModFileId.New()
                    });

        }

        public override AModFile? Alter(AModFile file)
        {
            if (file is IToFile to)
            {
                if (_removeFiles.Contains(to.To)) return null;
            }
            return base.Alter(file);
        }

        public override Mod? Alter(Mod mod)
        {
            var toAdd = _replaceFiles[mod.Id].Concat(_createFiles[mod.Id]);
            if (toAdd.Any())
            {
                mod = mod with
                {
                    Files = mod.Files.With(toAdd, f => f.Id)
                };
            }
            return base.Alter(mod);
        }
    }


}

