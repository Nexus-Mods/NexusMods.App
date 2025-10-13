using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// A factory class for creating various types of dialogs.
/// </summary>
public static class DialogFactory
{
    /// <summary>
    /// Creates a standard dialog with the specified title, parameters, button definitions, and window size.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="standardDialogParameters">The parameters for the standard dialog content.</param>
    /// <param name="buttonDefinitions">An array of button definitions for the dialog.</param>
    /// <param name="windowSize">The size of the dialog window. Defaults to <see cref="DialogWindowSize.Medium"/>.</param>
    /// <returns>A <see cref="Dialog"/> instance configured with the specified parameters.</returns>
    public static Dialog CreateStandardDialog(string title, StandardDialogParameters standardDialogParameters, DialogButtonDefinition[] buttonDefinitions, DialogWindowSize windowSize = DialogWindowSize.Medium)
    {
        var contentViewModel = new DialogStandardContentViewModel(standardDialogParameters);

        return CreateDialog(title, buttonDefinitions, contentViewModel, windowSize);
    }

    /// <summary>
    /// Creates a dialog with the specified title, button definitions, content view model, and window size.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="buttonDefinitions">An array of button definitions for the dialog.</param>
    /// <param name="contentViewModel">The content view model to be displayed in the dialog.</param>
    /// <param name="dialogWindowSize">The size of the dialog window. Defaults to <see cref="DialogWindowSize.Medium"/>.</param>
    /// <param name="showChrome">Show titlebar and close button. Defaults to true.</param>
    /// <returns>A <see cref="Dialog"/> instance configured with the specified parameters.</returns>
    public static Dialog CreateDialog(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        IViewModelInterface contentViewModel,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Medium,
        bool showChrome = true)
    {
        var viewModel = new DialogViewModel(title, buttonDefinitions, contentViewModel,
            dialogWindowSize, showChrome
        );

        return new Dialog(viewModel);
    }
}
