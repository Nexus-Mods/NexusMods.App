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

    public static MessageBoxViewModel MessageBoxDesignViewModel { get; } = new(
        "Delete this mod?",
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel]
    );

    public static MessageBoxViewModel MessageBoxDesignWithIconViewModel { get; } = new(
        "Delete this mod?",
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
        null,
        IconValues.PictogramSettings,
        MessageBoxSize.Medium
    );

    public static MessageBoxViewModel MessageBoxCustomDesignViewModel { get; } = new(
        "Delete this mod?",
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
        null,
        IconValues.PictogramSettings,
        MessageBoxSize.Medium,
        CustomContentDesignViewModel
    );

    public static MessageBoxViewModel MessageBoxCustomNoButtonsDesignViewModel { get; } = new(
        "Delete this mod?",
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [],
        null,
        null,
        MessageBoxSize.Large,
        CustomContentDesignViewModel
    );

    public static MessageBoxViewModel MessageBoxMarkdownDesignViewModel { get; } = new(
        "Title",
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Yes, MessageBoxStandardButtons.Cancel],
        "THIS IS A HEADING?",
        IconValues.PictogramSettings,
        MessageBoxSize.Medium,
        null,
        new MarkdownRendererViewModel
        {
            // Contents = """
            //     ## This is a markdown message box
            //     
            //     This is an example of a markdown message box.
            //     
            //     You can use **bold** and *italic* text.
            //     
            //     You can also use [links](https://www.nexusmods.com).
            //     """
            Contents = MarkdownRendererViewModel.DebugText,
        }
    );
}
