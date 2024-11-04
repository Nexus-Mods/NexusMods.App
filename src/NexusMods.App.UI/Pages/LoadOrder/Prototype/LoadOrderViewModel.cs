using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.UI;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public ReadOnlyObservableCollection<IObservableSortableItemViewModel> SortableItems { get; }
    public LoadOrderViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId, IObservableSortableItemProvider sortableItemProvider)
    {
        var sortableItems = new ObservableCollectionExtended<IObservableSortableItemViewModel>();
        
        sortableItemProvider.GetItems(Observable.Return(loadoutId))
            .SortBy(item => item.SortIndex)
            .Bind(sortableItems)
            .Subscribe();
        
        SortableItems = new ReadOnlyObservableCollection<IObservableSortableItemViewModel>(sortableItems);
    }

}
