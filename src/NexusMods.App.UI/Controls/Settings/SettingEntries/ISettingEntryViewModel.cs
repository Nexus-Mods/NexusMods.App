using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.MarkdownRenderer;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingEntryViewModel : IViewModelInterface
{
    ISettingsPropertyUIDescriptor PropertyUIDescriptor { get; }

    ISettingInteractionControl InteractionControlViewModel { get; }

    IMarkdownRendererViewModel? LinkRenderer { get; }
}
