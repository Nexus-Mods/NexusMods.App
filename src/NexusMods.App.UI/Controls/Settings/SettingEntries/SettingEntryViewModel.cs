using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Settings;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingEntryViewModel : AViewModel<ISettingEntryViewModel>, ISettingEntryViewModel
{
    public PropertyConfig Config { get; }

    public IInteractionControl InteractionControlViewModel { get; }

    public IMarkdownRendererViewModel? LinkRenderer { get; }
    public IMarkdownRendererViewModel DescriptionMarkdownRenderer { get; }

    public SettingEntryViewModel(
        PropertyConfig propertyConfig,
        IInteractionControl interactionControlViewModel,
        IMarkdownRendererViewModel descriptionMarkdownRenderer,
        IMarkdownRendererViewModel? linkRenderer)
    {
        Config = propertyConfig;

        InteractionControlViewModel = interactionControlViewModel;
        LinkRenderer = linkRenderer;
        DescriptionMarkdownRenderer = descriptionMarkdownRenderer;

        var link = propertyConfig.Options.HelpLink;
        if (link is not null && linkRenderer is not null)
        {
            const string markdown = "[Find out more]({0})";
            linkRenderer.Contents = string.Format(markdown, link.ToString());
        }

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InteractionControlViewModel.ValueContainer.CurrentValue)
                .Prepend(InteractionControlViewModel.ValueContainer.CurrentValue)
                .Select(value => propertyConfig.Options.DescriptionFactory.Invoke(value))
                .SubscribeWithErrorLogging(description => DescriptionMarkdownRenderer.Contents = description)
                .DisposeWith(disposables);
        });
    }
}
