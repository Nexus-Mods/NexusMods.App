using Avalonia.Controls;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

/// <summary>
/// Provides a factory for creating message boxes with specified properties.
/// </summary>
public static class MessageBoxFactory
{
    /// <summary>
    /// Creates a new message box with the specified title, text, button definitions, and size.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="text">The main content or message displayed in the message box.</param>
    /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
    /// <param name="messageBoxSize">The size of the message box (e.g., Small, Medium, or Large).</param>
    /// <returns>
    /// A <see cref="MessageBox{TView, TViewModel, TButtonId}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static MessageBox<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> Create(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize)
    {
        var viewModel = new MessageBoxViewModel(title, text, buttonDefinitions, messageBoxSize);
        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new MessageBox<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }
}
