using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Settings.SettingEntries.SettingInteractionControls;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryDesignViewModel : SettingEntryViewModel
{
    public SettingEntryDesignViewModel() : base(CreateDesignValues(), CreateInteractionControlViewModel()) { }

    private static ISettingsPropertyUIDescriptor CreateDesignValues()
    {
        return new SettingsPropertyUIDescriptor
        {
            SectionId = SectionId.NewId(),
            DisplayName = "Enable Telemetry",
            Description = "Send anonymous analytics information and usage data to Nexus Mods.",
            RequiresRestart = true,
            RestartMessage = "",
        };
    }

    private static IViewModelInterface CreateInteractionControlViewModel()
    {
        return new SettingToggleViewModel(new BooleanContainer(value: false, defaultValue: true));
    }

    private record SettingsPropertyUIDescriptor : ISettingsPropertyUIDescriptor
    {
        public required SectionId SectionId { get; init; }
        public required string DisplayName { get; init; }
        public required string Description { get; init; }
        public required bool RequiresRestart { get; init; }
        public required string? RestartMessage { get; init; }
        public SettingsPropertyValueContainer SettingsPropertyValueContainer => null!;
    }
}
