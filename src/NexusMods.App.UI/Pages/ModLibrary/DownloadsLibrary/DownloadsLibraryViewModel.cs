using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class DownloadsLibraryViewModel : APageViewModel<IDownloadsLibraryViewModel>, IDownloadsLibraryViewModel
{
    public DownloadsLibraryViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }
}
