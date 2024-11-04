using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Games.UI;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.Games.RedEngine.UI;

public class RedModObservableSortableItemProvider : RedModSortableItemProvider, IObservableSortableItemProvider
{
    public RedModObservableSortableItemProvider(IConnection connection) : base(connection)
    {
    }

    public IObservable<IChangeSet<IObservableSortableItemViewModel, EntityId>> GetItems(IObservable<LoadoutId> loadoutId)
    {
        return RedModLoadoutGroup.ObserveAll(Connection)
            .FilterOnObservable(group => loadoutId.Select(lid => lid == group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId))
            .Transform(group => new RedModSortableItem(this, group))
            .Transform(item => (IObservableSortableItemViewModel)new RedModObservableSortableItemViewModel(item));
    }
}
