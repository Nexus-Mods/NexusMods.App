using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    private readonly ReadOnlyObservableCollection<ISortableItemViewModel> _sortableItemViewModels;
    public ReadOnlyObservableCollection<ISortableItemViewModel> SortableItems => _sortableItemViewModels;
    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        var provider = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);

        var subscription = provider
            .SortableItems
            .ToObservableChangeSet()
            .Transform(item => (ISortableItemViewModel)new SortableItemViewModel(item))
            .Bind(out _sortableItemViewModels);

        this.WhenActivated(d =>
        {
            subscription.Subscribe()
                .DisposeWith(d);
        });
        
    }

}
