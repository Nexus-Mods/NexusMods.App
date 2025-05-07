using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ISortingSelectionViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }
    
    public IReadOnlyBindableReactiveProperty<bool> CanEdit { get; }
    
    public ReactiveCommand<NavigationInformation> OpenAllModsLoadoutPageCommand { get; }
}
