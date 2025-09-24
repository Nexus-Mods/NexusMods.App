using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingComboBoxViewModel : IInteractionControl
{
    string[] DisplayItems { get; }

    int SelectedItemIndex { get; set; }
}
