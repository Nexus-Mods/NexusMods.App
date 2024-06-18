using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.MarkdownRenderer;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    public ISettingInteractionControl InteractionControlViewModel { get; }

    public IMarkdownRendererViewModel? LinkRenderer { get; }

    public SettingEntryViewModel(
        ISettingsPropertyUIDescriptor propertyUIDescriptor,
        ISettingInteractionControl interactionControlViewModel,
        IMarkdownRendererViewModel? linkRenderer)
    {
        PropertyUIDescriptor = propertyUIDescriptor;
        InteractionControlViewModel = interactionControlViewModel;
        LinkRenderer = linkRenderer;

        var link = propertyUIDescriptor.Link;
        if (link is not null && linkRenderer is not null)
        {
            const string markdown = "[Find out more]({0})";
            linkRenderer.Contents = string.Format(markdown, link.ToString());
        }
    }
}
