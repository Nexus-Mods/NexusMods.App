using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    public ISettingInteractionControl InteractionControlViewModel { get; }

    public SettingEntryViewModel(
        ISettingsPropertyUIDescriptor propertyUIDescriptor,
        ISettingInteractionControl interactionControlViewModel)
    {
        PropertyUIDescriptor = propertyUIDescriptor;
        InteractionControlViewModel = interactionControlViewModel;
    }
}
