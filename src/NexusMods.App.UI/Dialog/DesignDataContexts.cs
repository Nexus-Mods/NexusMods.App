using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static CustomContentViewModel CustomContentDesignViewModel { get; } = new("Custom Text");
    
    public static MessageBoxViewModel MessageBoxDesignViewModel { get; } = new (
        "Delete this mod?", 
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel]
        );
    
    public static MessageBoxViewModel MessageBoxCustomDesignViewModel { get; } = new (
        "Delete this mod?", 
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
        MessageBoxSize.Medium,
        CustomContentDesignViewModel
    );
    
}
