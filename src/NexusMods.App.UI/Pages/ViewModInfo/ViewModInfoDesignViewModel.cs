using NexusMods.App.UI.Controls.ModInfo.ViewModFiles;

namespace NexusMods.App.UI.Pages.ViewModInfo;

public class ViewModInfoDesignViewModel : ViewModInfoViewModel
{
    // Design
    public ViewModInfoDesignViewModel() : base(null!, null!, null!)
    {
        PageViewModel = new ViewModFilesDesignViewModel();
    }
}
