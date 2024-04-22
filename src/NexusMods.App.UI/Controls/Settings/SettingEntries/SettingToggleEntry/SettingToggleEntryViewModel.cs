using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingToggleEntryViewModel : ASettingEntryViewModel
{
    public SettingToggleEntryViewModel(ISettingsPropertyUIDescriptor settingDescriptor) : base(settingDescriptor)
    {
        var booleanContainer = settingDescriptor.SettingsPropertyValueContainer.AsT0;
        
        InteractionControlViewModel = new SettingToggleControlViewModel
        {
            Value = booleanContainer.CurrentValue,
        };
    }
}
