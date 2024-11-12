using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    private readonly ReadOnlyObservableCollection<ISortableItemViewModel> _sortableItemViewModels;
    public ReadOnlyObservableCollection<ISortableItemViewModel> SortableItems => _sortableItemViewModels;
    public LoadOrderViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        var provider = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);
        provider
            .SortableItems
            .ToObservableChangeSet()
            .Transform(item => (ISortableItemViewModel)new SortableItemViewModel(item))
            .Bind(out _sortableItemViewModels)
            .Subscribe();
    }

}
