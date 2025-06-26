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
    // public static Dialog<DialogView, DialogViewModel> CreateStandardDialog(
    //     string title,
    //     DialogButtonDefinition[] buttonDefinitions,
    //     string? text = null,
    //     IconValue? icon = null,
    //     DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
    //     string? heading = null,
    //     IMarkdownRendererViewModel? markdownRenderer = null,
    //     IViewModelInterface? contentViewModel = null)
    // {
    //     var viewModel = new MessageDialogViewModel(
    //         new DialogBaseModel(
    //             title,
    //             buttonDefinitions,
    //             text,
    //             heading,
    //             icon,
    //             dialogWindowSize,
    //             markdownRenderer,
    //             contentViewModel
    //         )
    //     );
    //
    //     var view = new DialogView { DataContext = viewModel };
    //
    //     return new Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId>(view, viewModel);
    // }
    
    
    
    public static Dialog<DialogView, DialogViewModel> CreateStandardDialog(string title, string text, DialogButtonDefinition[] buttonDefinitions)
    {
        // standard dialog so we can use the standard content view model
        var contentViewModel = new DialogStandardContentViewModel(text);
        
        return CreateDialog(title, buttonDefinitions, contentViewModel);
    }
    
    public static Dialog<DialogView, DialogViewModel> CreateDialog(string title, DialogButtonDefinition[] buttonDefinitions, IViewModelInterface contentViewModel, DialogWindowSize dialogWindowSize = DialogWindowSize.Medium)
    {
        var viewModel = new DialogViewModel(title, buttonDefinitions, contentViewModel, dialogWindowSize);
        var view = new DialogView { DataContext = viewModel };
        
        return new Dialog<DialogView, DialogViewModel>(view, viewModel);
    }
    
    // public static Dialog<DialogView, InputDialogViewModel, InputDialogResult> TestInputDialog =>
    //     CreateInputDialog(
    //         "Name your Collection",
    //         [
    //             DialogStandardButtons.Cancel,
    //             new DialogButtonDefinition(
    //                 "Create", 
    //                 ButtonDefinitionId.From("create"), 
    //                 ButtonAction.Accept,
    //                 ButtonStyling.Primary
    //             ),
    //         ],
    //         text: "This is the name that will appear in the left hand menu and on the Collections page.",
    //         inputLabel: "Collection name",
    //         inputWatermark: "e.g. My Armour Mods",
    //         dialogWindowSize: DialogWindowSize.Medium
    //     );
    //
    // /*
    //  * STANDARD MESSAGE BOX INCLUDING BUTTONS
    //  */
    //
    // /// <summary>
    // /// Creates a standard "Ok/Cancel" message box with the specified title and text.
    // /// </summary>
    // /// <param name="title">The title of the message box.</param>
    // /// <param name="text">The main content or message displayed in the message box.</param>
    // /// <returns>
    // /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    // /// </returns>
    // public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> CreateOkCancelMessageBox(string title, string text)
    // {
    //     return CreateMessageDialog(
    //         title: title,
    //         buttonDefinitions: [
    //             DialogStandardButtons.Ok,
    //             DialogStandardButtons.Cancel,
    //         ],
    //         text: text
    //     );
    // }
    //
    // /// <summary>
    // /// Creates a standard "Yes/No" message box with the specified title and text.
    // /// </summary>
    // /// <param name="title">The title of the message box.</param>
    // /// <param name="text">The main content or message displayed in the message box.</param>
    // /// <returns>
    // /// A <see cref="Dialog{TView,TViewModel,T}"/> instance containing the View and ViewModel for the message box.
    // /// </returns>
    // public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> CreateYesNoMessageBox(string title, string text)
    // {
    //     return CreateMessageDialog(
    //         title: title,
    //         buttonDefinitions: [
    //             DialogStandardButtons.Yes,
    //             DialogStandardButtons.No,
    //         ],
    //         text: text
    //     );
    // }
    //
    //
    //
    // public static Dialog<DialogView, InputDialogViewModel, InputDialogResult> CreateInputDialog(
    //     string title,
    //     DialogButtonDefinition[] buttonDefinitions,
    //     string? text = null,
    //     string? inputText = null,
    //     string? inputLabel = null,
    //     string? inputWatermark = null,
    //     IconValue? icon = null,
    //     DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
    //     string? heading = null,
    //     IMarkdownRendererViewModel? markdownRenderer = null,
    //     IViewModelInterface? contentViewModel = null)
    // {
    //     var viewModel = new InputDialogViewModel(
    //         new DialogBaseModel(
    //             title,
    //             buttonDefinitions,
    //             text,
    //             heading,
    //             icon,
    //             dialogWindowSize,
    //             markdownRenderer,
    //             contentViewModel
    //         ),
    //         inputText,
    //         inputLabel,
    //         inputWatermark
    //     );
    //
    //     var view = new DialogView { DataContext = viewModel };
    //
    //     return new Dialog<DialogView, InputDialogViewModel, InputDialogResult>(view, viewModel);
    // }
    //
    // public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> CreateMessageDialog(
    //     string title,
    //     DialogButtonDefinition[] buttonDefinitions,
    //     string? text = null,
    //     IconValue? icon = null,
    //     DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
    //     string? heading = null,
    //     IMarkdownRendererViewModel? markdownRenderer = null,
    //     IViewModelInterface? contentViewModel = null)
    // {
    //     var viewModel = new MessageDialogViewModel(
    //         new DialogBaseModel(
    //             title,
    //             buttonDefinitions,
    //             text,
    //             heading,
    //             icon,
    //             dialogWindowSize,
    //             markdownRenderer,
    //             contentViewModel
    //         )
    //     );
    //
    //     var view = new DialogView { DataContext = viewModel };
    //
    //     return new Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId>(view, viewModel);
    // }
}
