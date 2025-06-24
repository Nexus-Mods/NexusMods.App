using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

public class DialogBaseModel : IDialogBaseModel
{
    public string Title { get; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public string? Text { get; }
    public string? Heading { get; }
    public IconValue? Icon { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public IMarkdownRendererViewModel? MarkdownRenderer { get; }
    public IViewModelInterface? ContentViewModel { get; }

    public DialogBaseModel(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        string? text = null,
        string? heading = null,
        IconValue? icon = null,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
        IMarkdownRendererViewModel? markdownRenderer = null,
        IViewModelInterface? contentViewModel = null)
    {
        Title = title;
        ButtonDefinitions = buttonDefinitions;
        Text = text;
        Heading = heading;
        Icon = icon;
        DialogWindowSize = dialogWindowSize;
        MarkdownRenderer = markdownRenderer;
        ContentViewModel = contentViewModel;
    }
}
