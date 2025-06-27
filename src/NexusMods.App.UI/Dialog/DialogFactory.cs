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
    public static Dialog CreateStandardDialog(string title, StandardDialogParameters standardDialogParameters, DialogButtonDefinition[] buttonDefinitions, DialogWindowSize windowSize = DialogWindowSize.Medium)
    {
        var contentViewModel = new DialogStandardContentViewModel(standardDialogParameters);

        return CreateDialog(title, buttonDefinitions, contentViewModel, windowSize);
    }
    
    /// <summary>
    /// Creates a dialog with the specified title, button definitions, content view model, and window size.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="buttonDefinitions"></param>
    /// <param name="contentViewModel"></param>
    /// <param name="dialogWindowSize"></param>
    /// <returns></returns>
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
