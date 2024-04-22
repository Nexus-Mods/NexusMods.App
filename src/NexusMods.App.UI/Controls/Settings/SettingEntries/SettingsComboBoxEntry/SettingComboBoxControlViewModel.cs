using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.SettingsComboBoxEntry;

public class SettingComboBoxControlViewModel : AViewModel<ISettingComboBoxControlViewModel>, ISettingComboBoxControlViewModel
{
    public string[] Items { get; set; }
    
    [Reactive] public string SelectedItem { get; set; }
    
    public SettingComboBoxControlViewModel(string[] items, string selectedItem)
    {
        Items = items;
        SelectedItem = selectedItem;
    }
}
