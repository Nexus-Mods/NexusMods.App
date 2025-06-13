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
    /*
     * STANDARD MESSAGE BOX INCLUDING BUTTONS
     */

    /// <summary>
    /// Creates a standard "Ok/Cancel" message box with the specified title and text.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="text">The main content or message displayed in the message box.</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateOkCancelMessageBox(string title, string text)
    {
        return CreateDialog(
            title,
            [
                DialogStandardButtons.Ok,
                DialogStandardButtons.Cancel,
            ],
            text
        );
    }

    /// <summary>
    /// Creates a standard "Yes/No" message box with the specified title and text.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="text">The main content or message displayed in the message box.</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateYesNoMessageBox(string title, string text)
    {
        return CreateDialog(
            title,
            [
                DialogStandardButtons.Yes,
                DialogStandardButtons.No,
            ],
            text
        );
    }

    /*
     * COMMON MESSAGE BOX OVERLOADS
     */

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        DialogButtonDefinition[] buttonDefinitions)
    {
        return CreateDialog(title, buttonDefinitions, text);
    }

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        DialogButtonDefinition[] buttonDefinitions,
        IconValue? icon)
    {
        return CreateDialog(title, buttonDefinitions, text,
            icon
        );
    }

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        DialogButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        DialogWindowSize dialogWindowSize)
    {
        return CreateDialog(title, buttonDefinitions, text,
            icon, dialogWindowSize
        );
    }

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        string? heading,
        DialogButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        DialogWindowSize dialogWindowSize,
        IMarkdownRendererViewModel? markdownRenderer)
    {
        return CreateDialog(title, buttonDefinitions, text,
            icon, dialogWindowSize, heading,
            markdownRenderer
        );
    }
    
    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        string heading,
        DialogButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        DialogWindowSize dialogWindowSize,
        IMarkdownRendererViewModel? markdownRenderer,
        IViewModelInterface? contentViewModel)
    {
        return CreateDialog(title, buttonDefinitions, text,
            icon, dialogWindowSize, heading,
            markdownRenderer, contentViewModel
        );
    }

    /// <summary>
    /// Creates a dialog with the specified parameters, including title, buttons, optional text, icon, size, heading, 
    /// markdown renderer, and content view model.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="buttonDefinitions">An array of <see cref="DialogButtonDefinition"/> specifying the buttons to include in the message box.</param>
    /// <param name="text">Optional. The main content or message displayed in the message box. Defaults to null.</param>
    /// <param name="icon">Optional. An icon to display in the message box. Defaults to null.</param>
    /// <param name="dialogWindowSize">Optional. The size of the message box (e.g., Small, Medium, or Large). Defaults to <see cref="DialogWindowSize.Small"/>.</param>
    /// <param name="heading">Optional. A heading to display in the message box. Defaults to null.</param>
    /// <param name="markdownRenderer">Optional. A markdown renderer ViewModel for rendering markdown content in the message box. Defaults to null.</param>
    /// <param name="contentViewModel">Optional. A custom content ViewModel to display in the message box. Defaults to null.</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    private static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> CreateDialog(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        string? text = null,
        IconValue? icon = null,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
        string? heading = null,
        IMarkdownRendererViewModel? markdownRenderer = null,
        IViewModelInterface? contentViewModel = null)
    {
        var viewModel = new DialogViewModel(
            title,
            buttonDefinitions,
            text,
            heading,
            icon,
            dialogWindowSize,
            markdownRenderer,
            contentViewModel
        );

        var view = new DialogView { DataContext = viewModel };

        return new Dialog<DialogView, DialogViewModel, ButtonDefinitionId>(view, viewModel);
    }
}
