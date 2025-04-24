using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersWipPageViewModel : APageViewModel<ILoadOrdersWIPPageViewModel>, ILoadOrdersWIPPageViewModel
{
    public ISortingSelectionViewModel SortingSelectionViewModel { get; }

    public LoadOrdersWipPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        SortingSelectionViewModel = new SortingSelectionViewModel(serviceProvider, loadoutId);
        TabTitle = "Load Orders";
    }
}
