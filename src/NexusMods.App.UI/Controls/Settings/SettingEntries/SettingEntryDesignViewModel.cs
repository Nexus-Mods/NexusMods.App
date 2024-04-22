using NexusMods.App.UI.Controls.Settings.SettingEntries.SettingInteractionControls;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryDesignViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public string DisplayName { get; } = "Enable Telemetry";
    public string Description { get; } = "Send anonymous analytics information and usage data to Nexus Mods.";
    public bool RequiresRestart { get; } = true;
    
    public IViewModelInterface InteractionControlViewModel { get; } = new SettingToggleViewModel();
}
