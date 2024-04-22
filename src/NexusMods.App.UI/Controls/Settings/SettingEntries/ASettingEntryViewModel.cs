using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public abstract class ASettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public string DisplayName { get; }
    public string Description { get; }
    public bool RequiresRestart { get; }
    public IViewModelInterface? InteractionControlViewModel { get; set; }
    
    protected ASettingEntryViewModel(ISettingsPropertyUIDescriptor settingDescriptor)
    {
        DisplayName = settingDescriptor.DisplayName;
        Description = settingDescriptor.Description;
        RequiresRestart = settingDescriptor.RequiresRestart;
    }
}
