using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Settings;

public class SettingsDesignViewModel : APageViewModel<ISettingsViewModel>, ISettingsViewModel
{
    public SettingsDesignViewModel() : base(new DesignWindowManager())
    {
    }
}
