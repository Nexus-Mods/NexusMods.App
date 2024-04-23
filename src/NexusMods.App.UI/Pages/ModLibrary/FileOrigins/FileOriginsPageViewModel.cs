using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    public FileOriginsPageViewModel(
        ILoadoutRegistry loadoutRegistry,
        
        IWindowManager windowManager) : base(windowManager)
    {
        
    }
}
