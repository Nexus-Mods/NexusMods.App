using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingInteractionControl : IViewModelInterface
{
    IValueContainer ValueContainer { get; }
}
