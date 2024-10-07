using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.UI;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.UI;

public class RedModObservableSortableItemProvider : RedModSortableItemProvider
{
    public RedModObservableSortableItemProvider(IConnection connection) : base(connection)
    {
    }

    public IObservable<IChangeSet<ISortableItem, EntityId>> GetItems(IObservable<LoadoutId> loadoutId)
    {
        return RedModLoadoutGroup.ObserveAll(Connection)
            .FilterOnObservable(group => loadoutId.Select(lid => lid == group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId))
            .Transform(group => (ISortableItem)new RedModSortableItem(null!, group));
    }
}
