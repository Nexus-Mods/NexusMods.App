using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games.UI;

public interface IObservableSortableItemProvider
{
    IObservable<IChangeSet<ISortableItem, EntityId>> GetItems(IObservable<LoadoutId> loadoutId);
}
