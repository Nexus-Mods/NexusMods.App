using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingInteractionControl : IViewModelInterface
{
    IValueContainer ValueContainer { get; }
}
