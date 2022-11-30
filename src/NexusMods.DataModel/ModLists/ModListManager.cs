using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.Interfaces;

namespace NexusMods.DataModel.ModLists;

public class ModListManager
{
    private readonly ILogger<ModListManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<ListRegistry> _root;

    public ModListManager(ILogger<ModListManager> logger, IDataStore store)
    {
        _logger = logger;
        _store = store;
        _root = new Root<ListRegistry>(RootType.ModLists, store);
    }

    public IObservable<ListRegistry> Changes => _root.Changes.Select(r => r.New);

    public ModListMarker ManageGame(GameInstallation installation, string name = "")
    {
        var n = ModList.Create() with { Name = name };
        _root.Alter(r => r with {Lists = r.Lists.With(n.ModListId, n)});
        return new ModListMarker(this, n.ModListId);
    }

    public void Alter(ModListId id, Func<ModList, ModList> func)
    {
        _root.Alter(r =>
        {
            var newList = func(r.Lists[id]);
            return r with { Lists = r.Lists.With(newList.ModListId, newList) };
        });
    }

    public ModList Get(ModListId id)
    {
        return _root.Value.Lists[id];
    }
}