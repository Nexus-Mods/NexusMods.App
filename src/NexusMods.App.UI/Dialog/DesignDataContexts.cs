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
    
    public static MessageBoxViewModel MessageBoxDesignViewModelExampleSmall { get; } = new(
        "Delete this mod?",
        [
            MessageBoxStandardButtons.Cancel,
            new MessageBoxButtonDefinition(
                "Yes, delete",
                ButtonDefinitionId.From("yes-delete"),
                ButtonAction.Accept,
                ButtonStyling.Destructive
            )
        ],
        "Deleting this mod will remove it from all collections. This action cannot be undone."
    );
    
    public static MessageBoxViewModel MessageBoxDesignViewModelExampleMedium { get; } = new(
        "Delete this mod?",
        [
            MessageBoxStandardButtons.Cancel,
            new MessageBoxButtonDefinition(
                "Find out more",
                ButtonDefinitionId.From("find-out-more")
            ),
            new MessageBoxButtonDefinition(
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

    public static MessageBoxViewModel MessageBoxDesignWithIconViewModel { get; } = new(
        "Delete this mod?",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        null,
        IconValues.PictogramSettings,
        MessageBoxSize.Medium
    );

    public static MessageBoxViewModel MessageBoxCustomDesignViewModel { get; } = new(
        "Delete this mod?",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        null,
        IconValues.PictogramSettings,
        MessageBoxSize.Medium,
        null,
        CustomContentDesignViewModel
    );

    public static MessageBoxViewModel MessageBoxCustomNoButtonsDesignViewModel { get; } = new(
        "Delete this mod?",
        [],
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        "Heading",
        IconValues.PictogramSettings,
        MessageBoxSize.Large,
        null,
        CustomContentDesignViewModel
    );

    public static MessageBoxViewModel MessageBoxMarkdownDesignViewModel { get; } = new(
        "Title",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Yes, MessageBoxStandardButtons.Cancel],
        "",
        "  ",
        null,
        MessageBoxSize.Medium,
        MarkdownRendererDesignViewModel
    );
}
