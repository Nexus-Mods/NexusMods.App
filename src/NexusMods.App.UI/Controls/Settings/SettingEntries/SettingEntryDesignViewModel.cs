using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryDesignViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }
    public IMarkdownRendererViewModel? DescriptionMarkdownRenderer { get; }
    public ISettingInteractionControl InteractionControlViewModel { get; }
    public IMarkdownRendererViewModel? LinkRenderer { get; }
    
    
    //public SettingEntryDesignViewModel() : base(CreateDesignValues(), CreateInteractionControlViewModel(), linkRenderer: null) { }

    public SettingEntryDesignViewModel()
    {
        PropertyUIDescriptor = CreateDesignValues();
        InteractionControlViewModel = CreateInteractionControlViewModel();
        
        // Set the link renderer to a dummy value for design purposes.
        LinkRenderer = new MarkdownRendererViewModel { Contents = $"[Find out more]({PropertyUIDescriptor.Link!.ToString()})" };
        
        // Set the description markdown contents to the property description.
        DescriptionMarkdownRenderer = new MarkdownRendererViewModel { Contents = "Send **anonymous analytics information** and usage data to Nexus Mods." };
    }

    private static ISettingsPropertyUIDescriptor CreateDesignValues()
    {
        return new SettingsPropertyUIDescriptor
        {
            SectionId = SectionId.NewId(),
            DisplayName = "Enable Telemetry",
            DescriptionFactory = _ => "Send anonymous analytics information and usage data to Nexus Mods.",
            Link = new Uri("https://www.example.org"),
            RequiresRestart = true,
            RestartMessage = null,
        };
    }

    private static ISettingInteractionControl CreateInteractionControlViewModel()
    {
        return new SettingToggleViewModel(new BooleanContainer(value: false, defaultValue: true, (_, _) => {}));
    }

    private record SettingsPropertyUIDescriptor : ISettingsPropertyUIDescriptor
    {
        public required SectionId SectionId { get; init; }
        public required string DisplayName { get; init; }
        public required Func<object, string> DescriptionFactory { get; init; }
        public required Uri? Link { get; init; }
        public required bool RequiresRestart { get; init; }
        public required string? RestartMessage { get; init; }
        public SettingsPropertyValueContainer SettingsPropertyValueContainer => null!;
    }

}
