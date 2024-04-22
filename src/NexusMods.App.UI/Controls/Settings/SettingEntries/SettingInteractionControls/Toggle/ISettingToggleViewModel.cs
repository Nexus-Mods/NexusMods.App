using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingToggleViewModel : ISettingInteractionControl
{
    BooleanContainer ValueContainer { get; }

    bool HasChanged { get; }
}
