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
    /// Creates a new message box with the specified title, text, button definitions, and size.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="text">The main content or message displayed in the message box.</param>
    /// <param name="icon">An optional icon to display in the message box.</param>
    /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
    /// <param name="messageBoxSize">The size of the message box (e.g., Small, Medium, or Large).</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        string text,
        IconValue? icon,
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize)
    {
        var viewModel = new MessageBoxViewModel(title, text,
            buttonDefinitions, null, null, messageBoxSize
        );
        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }

    /// <summary>
    /// Creates a new message box with the specified title, custom content, button definitions, and size.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="customContentViewModel">A custom content ViewModel to display in the message box.</param>
    /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
    /// <param name="messageBoxSize">The size of the message box (e.g., Small, Medium, or Large).</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
        string title,
        IDialogContentViewModel customContentViewModel,
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize)
    {
        var viewModel = new MessageBoxViewModel(title, "", 
            buttonDefinitions, null, null, messageBoxSize, customContentViewModel
        );
        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMarkdownMessageBox(
        string title,
        string text,
        IconValue? icon,
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize,
        IMarkdownRendererViewModel markdownRendererViewModel)
    {
        var viewModel = new MessageBoxViewModel(title, text,
            buttonDefinitions, null, icon, messageBoxSize, null,
            markdownRendererViewModel
        );

        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> YesNoMessageBox(
        string title,
        string text)
    {
        var viewModel = new MessageBoxViewModel(
            title,
            text,
            [MessageBoxStandardButtons.Yes, MessageBoxStandardButtons.No]
        );

        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }

    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> OkCancelMessageBox(
        string title,
        string text)
    {
        var viewModel = new MessageBoxViewModel(
            title,
            text,
            [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel]
        );

        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }
}
