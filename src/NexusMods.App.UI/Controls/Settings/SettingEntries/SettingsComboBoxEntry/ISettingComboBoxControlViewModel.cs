namespace NexusMods.App.UI.Controls.Settings.SettingEntries.SettingsComboBoxEntry;

public interface ISettingComboBoxControlViewModel : IViewModelInterface
{
    string[] Items { get; set; }
    string SelectedItem { get; set; }
}
