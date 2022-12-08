using System.Reactive.Linq;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.ModLists.ApplySteps;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.DataModel.RateLimiting;
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
        _manager.Alter(_id, list => list with {Mods = list.Mods.With(newMod)});
    }

    public async Task Install(AbsolutePath file, string name, CancellationToken token)
    {
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
                        continue;

                    yield return new BackupFile
                    {
                        To = path,
                        Hash = foundEntry.Hash,
                        Size = foundEntry.Size
                    };
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
            
            yield return new BackupFile
            {
                To = path,
                Hash = entry.Hash,
                Size = entry.Size
            };
            yield return new DeleteFile
            {
                To = path,
                Hash = entry.Hash,
                Size = entry.Size
            };
        }
    }
}