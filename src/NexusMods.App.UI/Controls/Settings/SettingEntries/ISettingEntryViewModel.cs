using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingEntryViewModel : IViewModelInterface
{
    ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }
    
    IMarkdownRendererViewModel? DescriptionMarkdownRenderer { get; }

    ISettingInteractionControl InteractionControlViewModel { get; }

    IMarkdownRendererViewModel? LinkRenderer { get; }
}
