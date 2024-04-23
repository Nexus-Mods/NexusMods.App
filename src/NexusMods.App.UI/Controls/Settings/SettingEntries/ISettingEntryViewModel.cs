using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingEntryViewModel : IViewModelInterface
{
    ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    ISettingInteractionControl InteractionControlViewModel { get; }
}
