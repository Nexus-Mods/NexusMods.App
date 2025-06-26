using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// Provides a factory for creating message boxes with specified properties.
/// </summary>
public static class DialogFactory
{
    public static Dialog CreateStandardDialog(string title, string text, DialogButtonDefinition[] buttonDefinitions)
    {
        // standard dialog so we can use the standard content view model
        var contentViewModel = new DialogStandardContentViewModel(new DialogParameters
        {
            Text = text,
        });

        return CreateDialog(title, buttonDefinitions, contentViewModel);
    }

    public static Dialog CreateDialog(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        IViewModelInterface contentViewModel,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Medium)
    {
        var viewModel = new DialogViewModel(title, buttonDefinitions, contentViewModel,
            dialogWindowSize
        );

        return new Dialog(viewModel);
    }
}
