using System.Reactive.Linq;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.ModLists.ApplySteps;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.Markers;

public class ModListMarker : IMarker<ModList>
{
    private readonly ModListManager _manager;
    private readonly ModListId _id;

    public ModListMarker(ModListManager manager, ModListId id)
    {
        _manager = manager;
        _id = id;
    }

    public void Alter(Func<ModList, ModList> f)
    {
        _manager.Alter(_id, f);
    }

    public ModList Value => _manager.Get(_id); 
    public IObservable<ModList> Changes => _manager.Changes.Select(c => c.Lists[_id]);

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

    public IEnumerable<ModList> History()
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
}