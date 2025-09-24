using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingToggleViewModel : IInteractionControl
{
    BooleanContainer BooleanContainer { get; }
}
