using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;

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
    /// <param name="buttonDefinitions">An array of <see cref="MessageBoxButtonDefinition"/> specifying the buttons to include in the message box.</param>
    /// <param name="messageBoxSize">The size of the message box (e.g., Small, Medium, or Large).</param>
    /// <returns>
    /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    /// </returns>
    public static Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> CreateMessageBox(
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

        return new Dialog<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }
    
    public static Dialog<DialogContainerView, DialogContainerViewModel, ButtonDefinitionId> CreateCustomDialog(string text)
    {
        
        // create custom content viewmodel
        var customViewModel = new CustomContentViewModel(text);

        // create dialog container viewmodel
        var containerViewModel = new DialogContainerViewModel(customViewModel, "Custom Content", 650, false);
        
        var view = new DialogContainerView()
        {
            DataContext = containerViewModel
        };

        return new Dialog<DialogContainerView, DialogContainerViewModel, ButtonDefinitionId>(view, containerViewModel);
    }

}
