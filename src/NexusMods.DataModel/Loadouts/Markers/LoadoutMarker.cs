using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting.Extensions;
using NexusMods.DataModel.Sorting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.Markers;

/// <summary>
/// Represents a mutable marker of a loadout. Operations on this class
/// may mutate a loadout, which will then cause a "rebase" of this marker
/// on the new loadoutID.
/// </summary>
public readonly struct LoadoutMarker
{
    private readonly LoadoutManager _manager;
    private readonly LoadoutId _id;

    /// <summary/>
    /// <param name="manager">Manager providing utility functionality for this marker.</param>
    /// <param name="id"></param>
    public LoadoutMarker(LoadoutManager manager, LoadoutId id)
    {
        _manager = manager;
        _id = id;
    }

    /// <summary>
    /// Gets the state of the loadout represented by the current ID.
    /// </summary>
    public Loadout Value => _manager.Registry.Get(_id)!;

    /// <summary>
    /// Gets the currently recognised tools for the game in this loadout.
    /// </summary>
    public IEnumerable<ITool> Tools => _manager.Tools(Value.Installation.Game.Domain);

    /// <summary>
    /// Installs a mod to a loadout with a given ID.
    /// </summary>
    /// <param name="file">Path of the file to be installed.</param>
    /// <param name="name">Name of the mod being installed.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <exception cref="Exception">No supported installer.</exception>
    /// <returns>Unique identifier for the new mod.</returns>
    /// <remarks>
    ///    In the context of NMA, 'install' currently means, analyze archives and
    ///    run archive through installers.
    ///    For more details, consider reading <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/AddingAGame.md#mod-installation">Adding a Game</a>.
    /// </remarks>
    public async Task<ModId> InstallModAsync(AbsolutePath file, string name, CancellationToken token = default)
    {
        return (await _manager.InstallModAsync(_id, file, name, token)).ModId;
    }

    /// <summary>
    /// Retrieves the list of all mods, sorts the mods and returns them as zipped tuples of (File, Mod).
    /// </summary>
    public IEnumerable<(AModFile File, Mod Mod)> FlattenList()
    {
        var list = _manager.Registry.Get(_id)!;
        var projected = new Dictionary<GamePath, (AModFile File, Mod Mod)>();
        var mods = Sorter.SortWithEnumerable(list.Mods.Values, i => i.Id, m => m.SortRules);
        foreach (var mod in mods)
        {
            foreach (var file in mod.Files.Values)
            {
                projected[file.To] = (file, mod);
            }
        }
        return projected.Values;
    }

    /// <summary>
    /// Returns all of the previous versions of this loadout for.
    /// </summary>
    public IEnumerable<Loadout> History()
    {
        var list = Value;
        // This exists to deal with bad data we may have for previous list versions
        // for example if we've purged the previous versions of a list
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (list != null)
        {
            yield return list;
            if (list.PreviousVersion.Id.Equals(IdEmpty.Empty))
                break;

            list = list.PreviousVersion.Value;
        }
    }

    /// <summary>
    /// Generates files for various game integrations, e.g. 'plugins.txt' for the Bethesda games.
    /// </summary>
    /// <param name="flattenedList">List returned from <see cref="FlattenList"/>.</param>
    /// <param name="token">Returned token used for cancellation.</param>
    public async Task<IEnumerable<(AModFile File, Mod Mod)>> GenerateFilesAsync(IReadOnlyCollection<(AModFile File, Mod Mod)> flattenedList, CancellationToken token = default)
    {
        var loadout = _manager.Registry.Get(_id)!;
        var generated = new List<(AModFile, Mod)>();
        var response = new List<(AModFile, Mod)>();
        foreach (var (file, mod) in flattenedList)
        {
            if (file is AGeneratedFile gen && gen.Hash == Hash.Zero)
            {
                var metaData = await gen.GetMetadataAsync(loadout, flattenedList, token);
                var newFile = (gen with { Hash = metaData.Hash, Size = metaData.Size }, mod);
                generated.Add(newFile);
                response.Add(newFile);
            }
            else
            {
                response.Add((file, mod));
            }
        }

        _manager.ReplaceFiles(_id, generated, $"Generated {generated.Count} files");
        return response;
    }

    /// <summary>
    /// Determines how we will push files out to game folder.<br/><br/>
    ///
    /// The 'apply plan' means:<br/>
    /// - Determine what steps we need to take to deploy mods to game folder.
    /// </summary>
    public async Task<ApplyPlan> MakeApplyPlanAsync(CancellationToken token = default)
    {
        var steps = new List<IApplyStep>();

        var list = _manager.Registry.Get(_id)!;
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFoldersAsync(list.Installation.Locations.Values, token)
            .ToDictionary(x => x.Path);

        var files = FlattenList().ToList();
        files = (await GenerateFilesAsync(files, token)).ToList();
        var flattenedList = files.ToDictionary(d => d.File.To.CombineChecked(gameFolders[d.File.To.Type]));

        var srcFiles = await srcFilesTask;

        foreach (var (path, (file, _)) in flattenedList)
        {
            if (file is AStaticModFile smf)
            {
                if (srcFiles.TryGetValue(path, out var foundEntry))
                {
                    if (foundEntry.Hash == smf.Hash && foundEntry.Size == smf.Size)
                    {
                        if (!_manager.ArchiveManager.HaveFile(smf.Hash))
                        {
                            steps.Add(new BackupFile
                            {
                                To = path,
                                Hash = smf.Hash,
                                Size = smf.Size
                            });
                        }
                        continue;
                    }

                    if (!_manager.ArchiveManager.HaveFile(foundEntry.Hash))
                    {
                        steps.Add(new BackupFile
                        {
                            To = path,
                            Hash = foundEntry.Hash,
                            Size = foundEntry.Size
                        });
                    }
                }

                steps.Add(new CopyFile
                {
                    To = path,
                    From = smf
                });
            }
        }

        foreach (var (path, entry) in srcFiles)
        {
            if (flattenedList.TryGetValue(path, out _))
                continue;

            if (!_manager.ArchiveManager.HaveFile(entry.Hash))
            {
                steps.Add(new BackupFile
                {
                    To = path,
                    Hash = entry.Hash,
                    Size = entry.Size
                });
            }

            steps.Add(new DeleteFile
            {
                To = path,
                Hash = entry.Hash,
                Size = entry.Size
            });
        }

        return new ApplyPlan
        {
            Files = files,
            Steps = steps
        };
    }

    /// <summary>
    /// Executes the plan specified by <see cref="MakeApplyPlanAsync"/>.
    /// </summary>
    /// <param name="plan">The plan to apply.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <exception cref="Exception">Some error occurred.</exception>
    public async Task ApplyAsync(ApplyPlan plan, CancellationToken token = default)
    {
        var loadout = _manager.Registry.Get(_id)!;

        LoadoutManager manager = _manager;

        await manager.Limiter.ForEachAsync(plan.Steps.OfType<BackupFile>().GroupBy(b => b.Hash),
            i => i.First().Size,
            async (j, itm) =>
            {
                var hash = await manager.ArchiveManager.ArchiveFileAsync(itm.First().To, token, j);
                if (hash != itm.Key)
                    throw new Exception("Archived file did not match expected hash");
            }, token);

        await _manager.Limiter.ForEachAsync(plan.Steps.OfType<DeleteFile>(), file => file.Size,
#pragma warning disable CS1998
            async (_, f) =>
#pragma warning restore CS1998
            {
                f.To.Delete();
            });

        var fromArchive = plan.Steps.OfType<CopyFile>().Select(f => (Step: f, From: f.From as FromArchive))
            .Where(f => f.From is not null)
            .GroupBy(f => f.From!.From.Hash);

        await manager.Limiter.ForEachAsync(fromArchive, x => x.Aggregate(Size.Zero, (acc, f) => acc + f.Step.Size),
            async (job, group) =>
            {
                var byPath = group.ToLookup(x => x.From!.From.RelativePath);
                await manager.ArchiveManager.ExtractAsync(group.Key, byPath.Select(e => e.Key),
                    async (path, sFn) =>
                    {
                        foreach (var entry in byPath[path])
                        {
                            await using var stream = await sFn.GetStreamAsync();
                            entry.Step.To.Parent.CreateDirectory();
                            await using var of = entry.Step.To.Create();
                            var hash = await stream.HashingCopyAsync(of, token, job);
                            if (hash != entry.Step.Hash)
                                throw new Exception("Unmatching hashes after installation");
                        }
                    }, token);
            });

        var generated = plan.Steps
            .OfType<CopyFile>()
            .Select(f => (Step: f, From: f.From as AGeneratedFile))
            .Where(f => f.From is not null);

        foreach (var (step, from) in generated)
        {
            await using var stream = step.To.Create();
            await from!.GenerateAsync(stream, loadout, plan.Files, token);
        }
    }

    /// <summary>
    /// Determines how we will integrate the files generated during game's runtime
    /// [e.g. saves, replays, logs] back as a game mod.<br/><br/>
    ///
    /// The 'ingestion plan' means:<br/>
    /// - Determine how we will integrate the newly generated files back into our DataModel.
    /// </summary>
    /// <param name="modMapper">Maps a given generated file to specified mod(s).</param>
    /// <param name="token">Cancel the task.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async IAsyncEnumerable<IApplyStep> MakeIngestionPlanAsync(Func<HashedEntry, Mod> modMapper, [EnumeratorCancellation] CancellationToken token)
    {
        var list = _manager.Registry.Get(_id)!;
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFoldersAsync(list.Installation.Locations.Values, token)
            .ToDictionary(x => x.Path);

        var flattenedList = FlattenList().ToDictionary(d => d.File.To.CombineChecked(gameFolders[d.File.To.Type]));
        var srcFiles = await srcFilesTask;

        foreach (var (absPath, (file, mod)) in flattenedList)
        {
            if (srcFiles.TryGetValue(absPath, out var found))
            {
                if (file is AStaticModFile sFile)
                {
                    if (found.Hash != sFile.Hash || found.Size != sFile.Size)
                    {
                        if (!_manager.ArchiveManager.HaveFile(found.Hash))
                        {
                            yield return new BackupFile
                            {
                                Hash = found.Hash,
                                Size = found.Size,
                                To = absPath
                            };
                        }

                        yield return new IntegrateFile
                        {
                            Mod = mod,
                            To = absPath,
                            Size = found.Size,
                            Hash = found.Hash,
                        };
                    }

                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                yield return new RemoveFromLoadout
                {
                    To = absPath
                };
            }
        }

        foreach (var (absolutePath, entry) in srcFiles)
        {
            if (flattenedList.ContainsKey(absolutePath)) continue;

            if (!_manager.ArchiveManager.HaveFile(entry.Hash))
            {
                yield return new BackupFile
                {
                    Hash = entry.Hash,
                    Size = entry.Size,
                    To = absolutePath
                };
            }

            yield return new AddToLoadout
            {
                To = absolutePath,
                Mod = modMapper(entry).Id,
                Hash = entry.Hash,
                Size = entry.Size
            };

        }
    }

    /// <summary>
    /// See <see cref="ApplyAsync(NexusMods.DataModel.Loadouts.Markers.LoadoutMarker.ApplyPlan,System.Threading.CancellationToken)"/>
    /// </summary>
    /// <param name="token">Cancel the task.</param>
    public async Task ApplyAsync(CancellationToken token = default)
    {
        await ApplyAsync(await MakeApplyPlanAsync(token), token);
    }

    /// <summary>
    /// See <see cref="ApplyIngest(System.Func{NexusMods.DataModel.HashedEntry,NexusMods.DataModel.Loadouts.Mod},System.Threading.CancellationToken)"/>
    /// </summary>
    /// <param name="modMapper">Maps a given generated file to specified mod(s).</param>
    /// <param name="token">Cancel the task.</param>
    public async Task ApplyIngest(Func<HashedEntry, Mod> modMapper, CancellationToken token)
    {
        await ApplyIngest(await MakeIngestionPlanAsync(modMapper, token).ToHashSetAsync(), token);
    }

    /// <summary>
    /// Applies the ingestion plan specified by <see cref="MakeApplyPlanAsync"/>.
    /// </summary>
    /// <param name="steps">The plan to apply.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <exception cref="Exception">Some error occurred.</exception>
    public async Task ApplyIngest(HashSet<IApplyStep> steps, CancellationToken token)
    {
        var manager = _manager;
        await manager.Limiter.ForEachAsync(steps.OfType<BackupFile>().GroupBy(b => b.Hash),
            i => i.First().Size,
            async (j, itm) =>
            {
                var hash = await manager.ArchiveManager.ArchiveFileAsync(itm.First().To, token, j);
                if (hash != itm.Key)
                    throw new Exception("Archived file did not match expected hash");
            }, token);


        Loadout ApplyLoadout(Loadout loadout)
        {
            foreach (var step in steps.Where(s => s is not BackupFile))
            {
                var gamePath = loadout.Installation.ToGamePath(step.To);
                switch (step)
                {
                    case AddToLoadout add:
                        loadout = loadout.Alter(add.Mod, m => m with
                        {
                            Files = m.Files.With(new FromArchive
                            {
                                Id = ModFileId.New(),
                                From = new HashRelativePath(add.Hash, default),
                                Hash = add.Hash,
                                Size = add.Size,
                                To = gamePath
                            }, x => x.Id)
                        });
                        break;

                    case RemoveFromLoadout:
                        loadout = loadout.AlterFiles(x => x.To == gamePath ? null : x);
                        break;
                    case IntegrateFile t:
                        var sourceArchive = manager.ArchiveManager.ArchivesThatContain(t.Hash).First();
                        loadout = loadout.Alter(t.Mod.Id, m => m with
                        {
                            Files = m.Files.With(new FromArchive
                            {
                                Id = ModFileId.New(),
                                // ReSharper disable once AccessToModifiedClosure
                                From = sourceArchive,
                                Hash = t.Hash,
                                Size = t.Size,
                                To = gamePath
                            }, x => x.Id)
                        });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return loadout;
        }

        _manager.Registry.Alter(_id, "Ingested changes from game folders", ApplyLoadout);
    }


    /// <summary>
    /// Adds a known mod to the given loadout.
    /// </summary>
    /// <param name="newMod">The mod to add to the loadout.</param>
    public void Add(Mod newMod)
    {
        _manager.Registry.Alter(_id, $"Added mod: {newMod.Name}", l => l.Add(newMod));
    }

    /// <summary>
    /// Runs the given tool, integrating the results into the loadout
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="token"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Run(ITool tool, CancellationToken token = default)
    {
        if (!tool.Domains.Contains(Value.Installation.Game.Domain))
            throw new Exception("Tool does not support this game");

        await ApplyAsync(token);
        await tool.Execute(Value);
        var modName = $"{tool.Name} Generated Files";
        var mod = Value.Mods.Values.FirstOrDefault(m => m.Name == modName) ??
                  new Mod
                  {
                      Name = modName,
                      Id = ModId.New(),
                      Files = EntityDictionary<ModFileId, AModFile>.Empty(_manager.Store)
                  };
        Add(mod);
        await ApplyIngest(_ => mod, token);
    }

    /// <summary>
    /// Alias for <see cref="LoadoutManager.ExportToAsync"/>.
    /// </summary>
    /// <param name="output">Path to which to save the loadout to.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    public async Task ExportToAsync(AbsolutePath output, CancellationToken token)
    {
        await _manager.ExportToAsync(_id, output, token);
    }

    /// <summary>
    /// Specifies the steps taken to deploy a collection of mods.
    /// </summary>
    public record ApplyPlan
    {
        /// <summary>
        /// Collection of all files involved in the application process.
        /// </summary>
        public required IReadOnlyList<(AModFile File, Mod mod)> Files { get; init; }

        /// <summary>
        /// The steps taken to deploy the collection.
        /// </summary>
        public required IReadOnlyList<IApplyStep> Steps { get; init; }
    }

    /// <summary>
    /// Alter the loadout.
    /// </summary>
    /// <param name="changeMessage"></param>
    /// <param name="func"></param>
    public void Alter(string changeMessage, Func<Loadout, Loadout> func)
    {
        _manager.Registry.Alter(_id, changeMessage, func);
    }
}
