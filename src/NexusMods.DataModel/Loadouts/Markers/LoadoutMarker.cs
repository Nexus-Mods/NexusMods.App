using System.Reactive.Linq;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting;
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
    public IObservable<Loadout> Changes => _manager.Changes.Select(c => c.Lists[_id]);

    public void Add(Mod newMod)
    {
        _manager.Alter(_id, list => list with {Mods = list.Mods.With(newMod)}, $"Added mod {newMod.Name}");
    }

    public async Task Install(AbsolutePath file, string name, CancellationToken token)
    {
        await _manager.ArchiveManager.ArchiveFile(file, token);
        await _manager.InstallMod(_id, file, name, token);
    }
    public IEnumerable<(AModFile File, Mod Mod)> FlattenList()
    {
        var list = _manager.Get(_id);
        var projected = new Dictionary<GamePath, (AModFile File, Mod Mod)>();
        foreach (var mod in list.Mods)
        {
            foreach (var file in mod.Files)
            {
                projected[file.To] = (file, mod);
            }
        }
        return projected.Values;
    }

    public IEnumerable<Loadout> History()
    {
        var list = Value;
        while (true)
        {
            yield return list;
            
            if (list.PreviousVersion.Id.Equals(IdEmpty.Empty))
                break;
            list = list.PreviousVersion.Value;
        }
    }

    public async IAsyncEnumerable<IApplyStep> MakeApplyPlan(CancellationToken token = default)
    {
        var list = _manager.Get(_id);
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFolders(list.Installation.Locations.Values, token)
            .ToDictionary(x => x.Path);

        var flattenedList = FlattenList().ToDictionary(d => d.File.To.RelativeTo(gameFolders[d.File.To.Folder]));
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
                            yield return new BackupFile
                            {
                                To = path,
                                Hash = smf.Hash,
                                Size = smf.Size
                            };
                        }
                        continue;
                    }

                    if (!_manager.ArchiveManager.HaveFile(foundEntry.Hash))
                    {
                        yield return new BackupFile
                        {
                            To = path,
                            Hash = foundEntry.Hash,
                            Size = foundEntry.Size
                        };
                    }
                }
                
                yield return new CopyFile
                {
                    To = path,
                    From = smf
                };
            }
        }

        foreach (var (path, entry) in srcFiles)
        {
            if (flattenedList.TryGetValue(path, out _)) 
                continue;

            if (!_manager.ArchiveManager.HaveFile(entry.Hash))
            {
                yield return new BackupFile
                {
                    To = path,
                    Hash = entry.Hash,
                    Size = entry.Size
                };
            }

            yield return new DeleteFile
            {
                To = path,
                Hash = entry.Hash,
                Size = entry.Size
            };
        }
    }

    public async Task ApplyPlan(IEnumerable<IApplyStep> steps, CancellationToken token = default)
    {
        await _manager.Limiter.ForEach(steps.OfType<BackupFile>().GroupBy(b => b.Hash),
            i => i.First().Size,
            async (j, itm) =>
            {
                var hash = await _manager.ArchiveManager.ArchiveFile(itm.First().To, token, j);
                if (hash != itm.Key)
                    throw new Exception("Archived file did not match expected hash");
            }, token);

        await _manager.Limiter.ForEach(steps.OfType<DeleteFile>(), file => file.Size,
            async (j, f) =>
            {
                f.To.Delete();
            });

        var fromArchive = steps.OfType<CopyFile>().Select(f => (Step: f, From: f.From as FromArchive))
            .Where(f => f.From is not null)
            .GroupBy(f => f.From!.From.Hash);

        await _manager.Limiter.ForEach(fromArchive, x => x.Sum(s => s.Step.Size),
            async (job, group) =>
            {
                var byPath = group.ToLookup(x => x.From.From.Parts.First());
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
    }

    
    public async IAsyncEnumerable<IApplyStep> MakeIngestionPlan(Func<HashedEntry, Mod> modMapper, CancellationToken token)
    {
        var list = _manager.Get(_id);
        var gameFolders = list.Installation.Locations;
        var srcFilesTask = _manager.FileHashCache
            .IndexFolders(list.Installation.Locations.Values, token)
            .ToDictionary(x => x.Path);
        
        var flattenedList = FlattenList().ToDictionary(d => d.File.To.RelativeTo(gameFolders[d.File.To.Folder]));
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
                Hash = entry.Hash,
                Size = entry.Size
            };

        }
    }

    public async Task Apply(CancellationToken token)
    {
        await ApplyPlan(await MakeApplyPlan(token).ToList(), token);
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
        
        
        Loadout Apply(Loadout modlist)
        {
            foreach (var step in steps.Where(s => s is not BackupFile))
            {
                var gamePath = modlist.Installation.ToGamePath(step.To);
                switch (step)
                {
                    case RemoveFromLoadout remove:
                        modlist = modlist.RemoveFileFromAllMods(x => x.To == gamePath);
                        break;
                    case IntegrateFile t:
                        var sourceArchive = _manager.ArchiveManager.ArchivesThatContain(t.Hash).First();
                        modlist = modlist.KeepMod(t.Mod, m => m with
                        {
                            Files = m.Files.With(new FromArchive
                            {
                                From = sourceArchive,
                                Hash = t.Hash,
                                Size = t.Size,
                                Store = m.Store,
                                To = gamePath
                            })
                        });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return modlist;
        }

        _manager.Alter(_id, Apply);
    }
}