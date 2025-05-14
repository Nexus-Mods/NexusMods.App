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
    
    /*
     * COMMON MESSAGE BOX OVERLOADS
     */
    
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

    /*
     * Catch all overload for message boxes.
     */
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        string heading,
        MessageBoxButtonDefinition[] buttonDefinitions,
        IconValue? icon,
        MessageBoxSize messageBoxSize,
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

    /// <summary>
        /// Creates a message box with the specified title, button definitions, content view model, and size.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
        /// <param name="contentViewModel">A custom content ViewModel to display in the message box.</param>
        /// <param name="messageBoxSize">The size of the message box (e.g., Small, Medium, or Large).</param>
        /// <returns>
        /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
        /// </returns>
        public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
            string title,
            MessageBoxButtonDefinition[] buttonDefinitions,
            IDialogContentViewModel contentViewModel,
            MessageBoxSize messageBoxSize
        )
        {
            return CreateMessageBoxInternal(title, buttonDefinitions, null,
                null, messageBoxSize, null,
                null, contentViewModel
            );
        }

    
    
    /// <summary>
        /// Creates a message box with the specified parameters, including title, buttons, optional text, icon, size, heading, 
        /// markdown renderer, and content view model.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
        /// <param name="text">Optional. The main content or message displayed in the message box. Defaults to null.</param>
        /// <param name="icon">Optional. An icon to display in the message box. Defaults to null.</param>
        /// <param name="messageBoxSize">Optional. The size of the message box (e.g., Small, Medium, or Large). Defaults to <see cref="MessageBoxSize.Small"/>.</param>
        /// <param name="heading">Optional. A heading to display in the message box. Defaults to null.</param>
        /// <param name="markdownRenderer">Optional. A markdown renderer ViewModel for rendering markdown content in the message box. Defaults to null.</param>
        /// <param name="contentViewModel">Optional. A custom content ViewModel to display in the message box. Defaults to null.</param>
        /// <returns>
        /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
        /// </returns>
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
