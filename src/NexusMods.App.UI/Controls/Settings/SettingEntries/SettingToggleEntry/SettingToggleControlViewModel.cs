using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingToggleControlViewModel : AViewModel<ISettingToggleControlViewModel>, ISettingToggleControlViewModel
{
    [Reactive]
    public bool Value { get; set; }
}
