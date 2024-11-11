using System.Collections.ObjectModel;

using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public ICollection<ISortableItemViewModel> SortableItems { get; }
    public LoadOrderViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        SortableItems = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId)
            .SortableItems
            .CreateWritableView( i => (ISortableItemViewModel)new SortableItemViewModel(i))
            .ToWritableNotifyCollectionChanged();
    }

}
