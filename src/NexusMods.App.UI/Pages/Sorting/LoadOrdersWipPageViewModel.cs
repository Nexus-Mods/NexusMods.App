using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersWipPageViewModel : APageViewModel<ILoadOrdersWIPPageViewModel>, ILoadOrdersWIPPageViewModel
{
    private readonly LoadoutId _loadoutId;

    public ISortingSelectionViewModel SortingSelectionViewModel { get; }


    public LoadOrdersWipPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadutId, IOSInterop iosInterop) : base(windowManager)
    {
        _loadoutId = loadutId;

        SortingSelectionViewModel = new SortingSelectionViewModel(serviceProvider, _loadoutId, iosInterop);
        
        TabTitle = "Load Orders";
    }

}
