using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.SettingsComboBoxEntry;

public class SettingComboBoxEntryViewModel : ASettingEntryViewModel
{
    public SettingComboBoxEntryViewModel(ISettingsPropertyUIDescriptor settingDescriptor) : base(settingDescriptor)
    {
        var multipleChoiceContainer = settingDescriptor.SettingsPropertyValueContainer.AsT1;

        InteractionControlViewModel = new SettingComboBoxControlViewModel(
            multipleChoiceContainer.Values.Select(kv => kv.Value).ToArray(),
            multipleChoiceContainer.Values.First(kv =>
                    kv.Key == multipleChoiceContainer.CurrentValue).Value
        );
    }
}
