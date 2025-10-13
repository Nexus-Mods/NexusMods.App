using NexusMods.App.UI.Controls.Navigation;
using NexusMods.UI.Sdk;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ISortingSelectionViewModel : IViewModelInterface
{
    IViewModelInterface[] ViewModels { get; }

    public IReadOnlyBindableReactiveProperty<bool> CanEdit { get; }
    
    public ReactiveCommand<NavigationInformation> OpenAllModsLoadoutPageCommand { get; }
}
