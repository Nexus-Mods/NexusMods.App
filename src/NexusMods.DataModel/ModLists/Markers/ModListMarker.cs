using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
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

    public async Task Install(AbsolutePath file, CancellationToken token)
    {
        await _manager.InstallMod(_id, file, token);
    }
}