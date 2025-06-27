using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static CustomContentExampleViewModel CustomContentExampleDesignViewModel { get; } = new("Custom Text");
    
    // create a static instance for a design view
    public static DialogViewModel StandardContent { get; } = new(
        "StandardContent Dialog Content",
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
        DialogWindowSize.Medium
    );

    // create a static instance for a design view
    public static DialogViewModel CustomContent { get; } = new(
        "CustomContent Dialog Content",
        [
            DialogStandardButtons.Yes,
            DialogStandardButtons.No,
        ],
        new CustomContentExampleViewModel("This is a custom content example view model for design-time purposes."),
        DialogWindowSize.Medium
    );
}
