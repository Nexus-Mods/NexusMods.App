using Avalonia.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// Provides a factory for creating message boxes with specified properties.
/// </summary>
public static class DialogFactory
{

    /// <summary>
    /// Creates a standard "Ok/Cancel" message box with the specified title and text.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="text">The main content or message displayed in the message box.</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateOkCancelMessageBox(string title, string text)
    {
        return CreateMessageBoxInternal(
            title,
            [
                MessageBoxStandardButtons.Ok,
                MessageBoxStandardButtons.Cancel,
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
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateYesNoMessageBox(string title, string text)
    {
        return CreateMessageBoxInternal(
            title,
            [
                MessageBoxStandardButtons.Yes,
                MessageBoxStandardButtons.No,
            ],
            text
        );
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, text);
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IconValue? icon)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, text,
            icon
        );
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        MessageBoxSize messageBoxSize)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, text,
            icon, messageBoxSize
        );
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        MessageBoxSize messageBoxSize,
        string heading)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, text,
            icon, messageBoxSize, heading
        );
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        MessageBoxSize messageBoxSize,
        string heading,
        IMarkdownRendererViewModel? markdownRenderer)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, text,
            icon, messageBoxSize, heading,
            markdownRenderer
        );
    }

    /*
     * These overloads are used when a content view model is provided. In this case, only the title and button definitions are used.
     * The content view model will be responsible for providing the content.
     */
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IDialogContentViewModel? contentViewModel)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, null,
            null, MessageBoxSize.Small, null,
            null, contentViewModel
        );
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IDialogContentViewModel? contentViewModel,
        MessageBoxSize messageBoxSize)
    {
        return CreateMessageBoxInternal(title, buttonDefinitions, null,
            null, messageBoxSize, null,
            null, contentViewModel
        );
    }

    private static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBoxInternal(
        string title,
        MessageBoxButtonDefinition[] buttonDefinitions,
        string? text = null,
        IconValue? icon = null,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small,
        string? heading = null,
        IMarkdownRendererViewModel? markdownRenderer = null,
        IDialogContentViewModel? contentViewModel = null)
    {
        var viewModel = new MessageBoxViewModel(
            title,
            buttonDefinitions,
            text,
            heading,
            icon,
            messageBoxSize,
            markdownRenderer,
            contentViewModel
        );

        var view = new MessageBoxView { DataContext = viewModel };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }
}
