using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    public ISettingInteractionControl InteractionControlViewModel { get; }

    public IMarkdownRendererViewModel? LinkRenderer { get; }
    public IMarkdownRendererViewModel? DescriptionMarkdownRenderer { get; }

    public SettingEntryViewModel(
        ISettingsPropertyUIDescriptor propertyUIDescriptor,
        ISettingInteractionControl interactionControlViewModel,
        IMarkdownRendererViewModel descriptionMarkdownRenderer,
        IMarkdownRendererViewModel? linkRenderer)
    {
        PropertyUIDescriptor = propertyUIDescriptor;
        InteractionControlViewModel = interactionControlViewModel;
        LinkRenderer = linkRenderer;
        DescriptionMarkdownRenderer = descriptionMarkdownRenderer;

        var link = propertyUIDescriptor.Link;
        if (link is not null && linkRenderer is not null)
        {
            const string markdown = "[Find out more]({0})";
            linkRenderer.Contents = string.Format(markdown, link.ToString());
        }

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InteractionControlViewModel.ValueContainer.CurrentValue)
                .Prepend(InteractionControlViewModel.ValueContainer.CurrentValue)
                .Select(value => PropertyUIDescriptor.DescriptionFactory.Invoke(value))
                .SubscribeWithErrorLogging(description => DescriptionMarkdownRenderer!.Contents = description)
                .DisposeWith(disposables);
        });
    }
}
