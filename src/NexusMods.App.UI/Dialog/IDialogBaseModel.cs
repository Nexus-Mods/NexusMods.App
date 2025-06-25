using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

public interface IDialogBaseModel
{
    string Title { get; }
    string? Text { get; }
    string? Heading { get; }
    IconValue? Icon { get; }
    DialogWindowSize DialogWindowSize { get; }
    IMarkdownRendererViewModel? MarkdownRenderer { get; }
    IViewModelInterface? ContentViewModel { get; }
    DialogButtonDefinition[] ButtonDefinitions { get; }
}
