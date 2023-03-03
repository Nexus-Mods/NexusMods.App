using System.IO.Compression;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.Markers;

public class LoadoutMarker : IMarker<Loadout>
{
    private readonly LoadoutManager _manager;
    private readonly LoadoutId _id;

    public LoadoutMarker(LoadoutManager manager, LoadoutId id)
    {
        _manager = manager;
        _id = id;
    }

    public void Alter(Func<Loadout, Loadout> f)
    {
        _manager.Alter(_id, f);
    }

    public Loadout Value => _manager.Get(_id); 
    public IEnumerable<ITool> Tools => _manager.Tools(Value.Installation.Game.Domain);
    public IObservable<Loadout> Changes => _manager.Changes.Select(c => c.Lists[_id]);

    public async Task<ModId> Install(AbsolutePath file, string name, CancellationToken token = default)
    {
        await _manager.ArchiveManager.ArchiveFile(file, token);
        return (await _manager.InstallMod(_id, file, name, token)).ModId;
    }
    public IEnumerable<(AModFile File, Mod Mod)> FlattenList()
    {
        var list = _manager.Get(_id);
        var projected = new Dictionary<GamePath, (AModFile File, Mod Mod)>();
        var mods = Sorter.SortWithEnumerable<Mod, ModId>(list.Mods.Values, i => i.Id, m => m.SortRules);
        foreach (var mod in mods)
        {
            foreach (var file in mod.Files.Values)
            {
                projected[file.To] = (file, mod);
            }
        }
        return projected.Values;
    }

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

    public async Task<IEnumerable<(AModFile File, Mod Mod)>> GenerateFiles(IReadOnlyCollection<(AModFile File, Mod Mod)> flattenedList, CancellationToken token = default)
    {
        var loadout = _manager.Get(_id);
        var generated = new List<(AModFile, Mod)>();
        var response = new List<(AModFile, Mod)>();
        foreach (var (file, mod) in flattenedList)
        {
            if (file is AGeneratedFile gen && gen.Hash == Hash.Zero)
            {
                var metaData = await gen.GetMetaData(loadout, flattenedList, token);
                var newFile = (gen with {Hash = metaData.Hash, Size = metaData.Size}, mod);
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

    public record ApplyPlan
    {
        public required IReadOnlyList<(AModFile File, Mod mod)> Files { get; init; }
        public required IReadOnlyList<IApplyStep> Steps { get; init; }
    }

    public async Task<ApplyPlan> MakeApplyPlan(CancellationToken token = default)
    {
        var steps = new List<IApplyStep>();

        var list = _manager.Get(_id);
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFolders(list.Installation.Locations.Values, token)
            .ToDictionary(x => x.Path);

        var files = FlattenList().ToList();
        files = (await GenerateFiles(files, token)).ToList();
        var flattenedList = files.ToDictionary(d => d.File.To.CombineChecked(gameFolders[d.File.To.Type]));
        
        var srcFiles = await srcFilesTask;

        foreach (var (path, (file, mod)) in flattenedList)
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

    public async Task Apply(ApplyPlan plan, CancellationToken token = default)
    {
        var loadout = _manager.Get(_id);
        
        await _manager.Limiter.ForEach(plan.Steps.OfType<BackupFile>().GroupBy(b => b.Hash),
            i => i.First().Size,
            async (j, itm) =>
            {
                var hash = await _manager.ArchiveManager.ArchiveFile(itm.First().To, token, j);
                if (hash != itm.Key)
                    throw new Exception("Archived file did not match expected hash");
            }, token);

        await _manager.Limiter.ForEach(plan.Steps.OfType<DeleteFile>(), file => file.Size,
#pragma warning disable CS1998
            async (j, f) =>
#pragma warning restore CS1998
            {
                f.To.Delete();
            });

        var fromArchive = plan.Steps.OfType<CopyFile>().Select(f => (Step: f, From: f.From as FromArchive))
            .Where(f => f.From is not null)
            .GroupBy(f => f.From!.From.Hash);

        await _manager.Limiter.ForEach(fromArchive, x => x.Aggregate(Size.Zero, (acc, f) => acc + f.Step.Size),
            async (job, group) =>
            {
                var byPath = group.ToLookup(x => x.From!.From.Parts.First());
                await _manager.ArchiveManager.Extract(group.Key, byPath.Select(e => e.Key),
                    async (path, sFn) =>
                    {
                        foreach (var entry in byPath[path])
                        {
                            await using var strm = await sFn.GetStream();
                            entry.Step.To.Parent.CreateDirectory();
                            await using var of = entry.Step.To.Create();
                            var hash = await strm.HashingCopy(of, token, job);
                            if (hash != entry.Step.Hash)
                                throw new Exception("Unmatching hashes after installation");
                        }
                    }, token);
            });
        
        var generated = plan.Steps.OfType<CopyFile>().Select(f => (Step: f, From: f.From as AGeneratedFile))
            .Where(f => f.From is not null);

        foreach (var (step, from) in generated)
        {
            await using var stream = step.To.Create();
            await from.GenerateAsync(stream, loadout, plan.Files, token);
        }
    }

    
    public async IAsyncEnumerable<IApplyStep> MakeIngestionPlan(Func<HashedEntry, Mod> modMapper, [EnumeratorCancellation] CancellationToken token)
    {
        var list = _manager.Get(_id);
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFolders(list.Installation.Locations.Values, token)
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

    public async Task Apply(CancellationToken token = default)
    {
        await Apply(await MakeApplyPlan(token), token);
    }

    public async Task ApplyIngest(Func<HashedEntry, Mod> modMapper, CancellationToken token)
    {
        await ApplyIngest(await MakeIngestionPlan(modMapper, token).ToHashSet(), token);
    }

    public async Task ApplyIngest(HashSet<IApplyStep> steps, CancellationToken token)
    {
        await _manager.Limiter.ForEach(steps.OfType<BackupFile>().GroupBy(b => b.Hash),
            i => i.First().Size,
            async (j, itm) =>
            {
                var hash = await _manager.ArchiveManager.ArchiveFile(itm.First().To, token, j);
                if (hash != itm.Key)
                    throw new Exception("Archived file did not match expected hash");
            }, token);
        
        
        Loadout Apply(Loadout Loadout)
        {
            foreach (var step in steps.Where(s => s is not BackupFile))
            {
                var gamePath = Loadout.Installation.ToGamePath(step.To);
                switch (step)
                {
                    case AddToLoadout add:
                        Loadout = Loadout.Alter(add.Mod, m => m with
                        {
                            Files = m.Files.With(new FromArchive
                            {
                                Id = ModFileId.New(),
                                From = new HashRelativePath(add.Hash),
                                Hash = add.Hash,
                                Size = add.Size,
                                Store = m.Store,
                                To = gamePath
                            }, x => x.Id)
                        });
                        break;
                        
                    case RemoveFromLoadout remove:
                        Loadout = Loadout.AlterFiles(x => x.To == gamePath ? null : x);
                        break;
                    case IntegrateFile t:
                        var sourceArchive = _manager.ArchiveManager.ArchivesThatContain(t.Hash).First();
                        Loadout = Loadout.Alter(t.Mod.Id, m => m with
                        {
                            Files = m.Files.With(new FromArchive
                            {
                                Id = ModFileId.New(),
                                From = sourceArchive,
                                Hash = t.Hash,
                                Size = t.Size,
                                Store = m.Store,
                                To = gamePath
                            }, x => x.Id)
                        });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return Loadout;
        }

        _manager.Alter(_id, Apply);
    }

    public void Alter(ModId mod, Func<Mod?, Mod?> fn)
    {
        _manager.Alter(_id, l => l.Alter(mod, fn));
    }

    public void Add(Mod newMod)
    {
        _manager.Alter(_id, l => l.Add(newMod));
    }

    /// <summary>
    /// Runs the given tool, integrating the results into the loadout
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="none"></param>
    /// <param name="token"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Run(ITool tool, CancellationToken token = default)
    {
        if (!tool.Domains.Contains(Value.Installation.Game.Domain))
            throw new Exception("Tool does not support this game");
        
        await Apply(token);
        await tool.Execute(Value);
        var modName = $"{tool.Name} Generated Files";
        var mod = Value.Mods.Values.FirstOrDefault(m => m.Name == modName) ??
                  new Mod
                  {
                      Name = modName,
                      Id = ModId.New(),
                      Store = Value.Store,
                      Files = EntityDictionary<ModFileId, AModFile>.Empty(Value.Store)
                  };
        Add(mod);
        await ApplyIngest(_ => mod, token);
    }

    public async Task ExportTo(AbsolutePath output, CancellationToken token)
    {
        await _manager.ExportTo(_id, output, token);
    }
}