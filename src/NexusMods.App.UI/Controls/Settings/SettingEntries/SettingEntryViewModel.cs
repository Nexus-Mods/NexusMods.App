using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    public IViewModelInterface InteractionControlViewModel { get; }

    public SettingEntryViewModel(
        ISettingsPropertyUIDescriptor propertyUIDescriptor,
        IViewModelInterface interactionControlViewModel)
    {
        PropertyUIDescriptor = propertyUIDescriptor;
        InteractionControlViewModel = interactionControlViewModel;
    }
}
