using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static CustomContentViewModel CustomContentDesignViewModel { get; } = new("Custom Text");

    public static MarkdownRendererViewModel MarkdownRendererDesignViewModel { get; } = new MarkdownRendererViewModel
    {
        Contents = """
            ## This is a markdown message box
            
            This is an example of a markdown message box.
            
            You can use **bold** and *italic* text.
            
            You can also use [links](https://www.nexusmods.com).
            """,
    };

    /*
     * The following message box examples are used for design purposes only. They are not intended to be used in production code.
     * https://www.figma.com/design/RGRSmIC4KoVlIosQB5YmQY/%F0%9F%93%B1%F0%9F%A7%B1-App-components?m=auto&node-id=2-1912
     */
    
    public static DialogViewModel DialogDesignViewModelExampleSmall { get; } = new(
        "Delete this mod?",
        [
            DialogStandardButtons.Cancel,
            new DialogButtonDefinition(
                "Yes, delete",
                ButtonDefinitionId.From("yes-delete"),
                ButtonAction.Accept,
                ButtonStyling.Destructive
            )
        ],
        "Deleting this mod will remove it from all collections. This action cannot be undone."
    );
    
    public static DialogViewModel DialogDesignViewModelExampleMedium { get; } = new(
        "Delete this mod?",
        [
            DialogStandardButtons.Cancel,
            new DialogButtonDefinition(
                "Find out more",
                ButtonDefinitionId.From("find-out-more")
            ),
            new DialogButtonDefinition(
                "Get Premium",
                ButtonDefinitionId.From("get-premium"),
                ButtonAction.Accept,
                ButtonStyling.Premium
            )
        ],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        "Get Premium",
        IconValues.Premium
    );

    public static DialogViewModel DialogDesignWithIconViewModel { get; } = new(
        "Delete this mod?",
        [DialogStandardButtons.Ok, DialogStandardButtons.Cancel],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        null,
        IconValues.PictogramSettings,
        DialogWindowSize.Medium
    );

    public static DialogViewModel DialogCustomDesignViewModel { get; } = new(
        "Delete this mod?",
        [DialogStandardButtons.Ok, DialogStandardButtons.Cancel],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        null,
        IconValues.PictogramSettings,
        DialogWindowSize.Medium,
        null,
        CustomContentDesignViewModel
    );

    public static DialogViewModel DialogCustomNoButtonsDesignViewModel { get; } = new(
        "Delete this mod?",
        [],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        "Heading",
        IconValues.PictogramSettings,
        DialogWindowSize.Large,
        null,
        CustomContentDesignViewModel
    );

    public static DialogViewModel DialogMarkdownDesignViewModel { get; } = new(
        "Title",
        [DialogStandardButtons.Ok, DialogStandardButtons.Yes, DialogStandardButtons.Cancel],
        "",
        "  ",
        null,
        DialogWindowSize.Medium,
        MarkdownRendererDesignViewModel
    );
}
