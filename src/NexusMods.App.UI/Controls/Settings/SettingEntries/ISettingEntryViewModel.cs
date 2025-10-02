using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingEntryViewModel : IViewModelInterface
{
    PropertyConfig Config { get; }

    IMarkdownRendererViewModel DescriptionMarkdownRenderer { get; }

    IInteractionControl InteractionControlViewModel { get; }

    IMarkdownRendererViewModel? LinkRenderer { get; }
}
