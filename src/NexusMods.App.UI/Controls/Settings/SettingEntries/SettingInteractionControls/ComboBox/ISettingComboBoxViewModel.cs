namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingComboBoxViewModel : ISettingInteractionControl
{
    string[] DisplayItems { get; }

    int SelectedItemIndex { get; set; }
}
