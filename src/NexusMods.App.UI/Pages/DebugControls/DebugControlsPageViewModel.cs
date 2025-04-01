using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
}

public class DebugControlsPageViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
        TabTitle = "Debug Controls";
        TabIcon = IconValues.ColorLens;
        
    }
}
