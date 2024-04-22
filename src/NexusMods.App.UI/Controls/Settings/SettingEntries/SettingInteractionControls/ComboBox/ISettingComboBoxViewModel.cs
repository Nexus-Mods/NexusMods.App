using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingComboBoxViewModel : ISettingInteractionControl
{
    SingleValueMultipleChoiceContainer ValueContainer { get; }

    string[] DisplayItems { get; }

    int SelectedItemIndex { get; }
}
