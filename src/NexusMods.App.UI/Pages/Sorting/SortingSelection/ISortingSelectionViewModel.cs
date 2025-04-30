using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ISortingSelectionViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }
    
    public IReadOnlyBindableReactiveProperty<bool> CanEdit { get; }
}
