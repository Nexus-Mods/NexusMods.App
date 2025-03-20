using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderDataProvider
{
    IObservable<IChangeSet<CompositeItemModel<Guid>, Guid>> ObserveLoadOrder(ILoadoutSortableItemProvider sortableItemProvider);
}
