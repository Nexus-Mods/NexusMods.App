using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersWipPageViewModel : APageViewModel<ILoadOrdersWIPPageViewModel>, ILoadOrdersWIPPageViewModel
{
    public ISortingSelectionViewModel SortingSelectionViewModel { get; }

    public LoadOrdersWipPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        SortingSelectionViewModel = new SortingSelectionViewModel(serviceProvider, loadoutId, Optional<Observable<bool>>.None);
        TabTitle = "Load Orders";
    }
}
