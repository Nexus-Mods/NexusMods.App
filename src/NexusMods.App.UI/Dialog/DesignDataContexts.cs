using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used for design views
/// </summary>
internal static class DesignDataContexts
{
    
    // create a static instance for a design view
    public static DialogViewModel StandardContent { get; } = new(
        "StandardContent Dialog",
        [
            DialogStandardButtons.Ok,
            DialogStandardButtons.Cancel,
        ],
        new DialogStandardContentViewModel(
            new StandardDialogParameters()
            {
                Text =
                    "This is a design-time dialog content view model. It is used to demonstrate the dialog's layout and functionality without requiring a full implementation of the content view model.",
                BottomText = "Bottom text can be used to provide additional information or context.",
            }
        ),
        DialogWindowSize.Medium,
        true
    );
    
    // create a content view model for a dialog that includes a markdown renderer
    public static DialogViewModel StandardContentWithMarkdown { get; } = new(
        "StandardContentWithMarkdown Dialog",
        [
            DialogStandardButtons.Ok,
            DialogStandardButtons.Cancel,
            new DialogButtonDefinition(
                "Go Premium",
                ButtonDefinitionId.From("go-premium"),
                ButtonAction.None,
                ButtonStyling.Premium
            ),
        ],
        new DialogStandardContentViewModel(
            new StandardDialogParameters()
            {
                Text = "This dialog content includes a markdown renderer.",
                Markdown = new MarkdownRendererViewModel()
                {
                    Contents = """
                        # Markdown Renderer Example
                        
                        This is an example of a markdown renderer in a dialog. It can render **bold**, *italic*, and [links](https://example.com).
                        
                        - Item 1
                        - Item 2
                        - Item 3
                        """,
                },
            }
        ),
        DialogWindowSize.Medium,
        true
    );

}
